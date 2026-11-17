#!/usr/bin/env python3
"""GodsWar login probe.

Two modes, both speaking the client's native protocol:

  --listen            Stand in for the LoginServer and print the first login
                      packet the real client sends. Use this to discover the
                      exact version number (fVersion) your client expects, then
                      put it in CLIENT_VERSION. The client sends the build as
                      1_000_000 + build (1.0.270 -> 1000270).

  --test              Full headless handshake against the running stack:
                      LoginServer -> server list -> GameServer validation.
                      Proves the server side of the chain works without the GUI.

The cipher is the XOR keystream from Net.dll's Encryptor (see
GW 2.0/Server Files (Source)/Common/Include/cNet/Encryptor.h). Both sides
generate two 256-byte polynomial tables and XOR them into the stream; send and
receive directions each start at position 0.
"""
import argparse
import socket
import struct
import sys
import time

# <a1, b1, c1, fst1, a2, b2, c2, fst2> as documented for the client encryptor.
COEFFS = (0xEE, 0x17, 0x33, 0x50, 0x82, 0x23, 0x61, 0x33)

MAX_NAME = 32
VALIDATE_CODE_LEN = 25

MSG_LOGIN = 1
MSG_LOGIN_RETURN_INFO = 2
MSG_REQUEST_GAMESERVER = 3
MSG_RESPONSE_GAMESERVER = 10001
MSG_ROLE_INFO = 10002


def _table(a, b, c, first):
    buf = bytearray(256)
    n = first
    for i in range(256):
        buf[i] = n
        n = (a * n * n + b * n + c) % 256
    return bytes(buf)


def keystream():
    a1, b1, c1, f1, a2, b2, c2, f2 = COEFFS
    t1 = _table(a1, b1, c1, f1)
    t2 = _table(a2, b2, c2, f2)
    return bytes(x ^ y for x, y in zip(t1, t2))


KS = keystream()


class Cipher:
    """Stateful XOR-stream: one instance per direction."""

    def __init__(self):
        self.pos = 0

    def apply(self, data: bytes) -> bytes:
        out = bytearray(data)
        for i in range(len(out)):
            out[i] ^= KS[self.pos]
            self.pos = (self.pos + 1) % 256
        return bytes(out)

    def peek(self, data: bytes) -> bytes:
        """Decrypt without consuming, for reading a packet header."""
        out = bytearray(data)
        pos = self.pos
        for i in range(len(out)):
            out[i] ^= KS[pos]
            pos = (pos + 1) % 256
        return bytes(out)


class BufferedSock:
    """Socket wrapper that keeps bytes left over from a recv().

    The shim writes the login reply and the pushed server list in one burst, so
    a plain recv(4096) returns both packets at once. Without buffering the
    second packet is thrown away and the read times out.
    """

    def __init__(self, sock):
        self.sock = sock
        self.buf = b""

    def settimeout(self, t):
        self.sock.settimeout(t)

    def recv(self, n):
        if self.buf:
            out, self.buf = self.buf[:n], self.buf[n:]
            return out
        return self.sock.recv(n)

    def sendall(self, data):
        self.sock.sendall(data)

    def read_exact(self, n, timeout=None):
        """Read exactly n bytes, keeping anything beyond them buffered."""
        if timeout is not None:
            self.sock.settimeout(timeout)
        while len(self.buf) < n:
            chunk = self.sock.recv(4096)
            if not chunk:
                break
            self.buf += chunk
        out, self.buf = self.buf[:n], self.buf[n:]
        return out

    def close(self):
        self.sock.close()


def recv_packet(sock, cipher, timeout=10.0):
    """Read one MsgHead-framed packet (plaintext header is encrypted too).

    Reads exactly one packet and leaves any following bytes buffered, so a
    server that writes two packets in one burst does not desync the cipher.
    """
    if isinstance(sock, BufferedSock):
        head_raw = sock.read_exact(4, timeout)
        if len(head_raw) < 4:
            return None, bytes(head_raw)
        head = cipher.peek(head_raw)
        size = struct.unpack_from("<H", head, 0)[0]
        if not (4 <= size <= 65535):
            size = 4
        rest = sock.read_exact(size - 4, timeout)
        raw = head_raw + rest
    else:
        sock.settimeout(timeout)
        raw = bytearray()
        need = 4
        while len(raw) < need:
            chunk = sock.recv(4096)
            if not chunk:
                break
            raw.extend(chunk)
            if len(raw) >= 4:
                head = cipher.peek(bytes(raw[:4]))
                size = struct.unpack_from("<H", head, 0)[0]
                need = size if 4 <= size <= 65535 else 4
        raw = bytes(raw)
    if len(raw) < 4:
        return None, bytes(raw)
    plain = cipher.apply(bytes(raw))
    size, mtype = struct.unpack_from("<HH", plain, 0)
    return {"size": size, "type": mtype, "raw": bytes(raw), "plain": plain}, bytes(raw)


def recv_packet_plain(sock, timeout=15.0):
    """Read one MsgHead-framed packet from a PLAINTEXT stream.

    The client->gameserver direction of this build is not encrypted, so the
    reply comes back plaintext as well. Decrypting it produces a garbage
    header and the read never completes.
    """
    sock.settimeout(timeout)
    raw = bytearray()
    need = 4
    while len(raw) < need:
        chunk = sock.recv(4096)
        if not chunk:
            break
        raw.extend(chunk)
        if len(raw) >= 4:
            size = struct.unpack_from("<H", raw, 0)[0]
            need = size if 4 <= size <= 65535 else 4
    if len(raw) < 4:
        return None, bytes(raw)
    size, mtype = struct.unpack_from("<HH", raw, 0)
    return {"size": size, "type": mtype, "raw": bytes(raw),
            "plain": bytes(raw)}, bytes(raw)


def send_packet(sock, cipher, payload: bytes):
    sock.sendall(cipher.apply(payload))


def login_packet(name: str, password: str, version: int) -> bytes:
    return struct.pack("<HH", 72, MSG_LOGIN) + \
        name.encode("ascii", "replace")[:MAX_NAME - 1].ljust(MAX_NAME, b"\0") + \
        password.encode("ascii", "replace")[:MAX_NAME - 1].ljust(MAX_NAME, b"\0") + \
        struct.pack("<i", version)


def request_gameserver_packet(name: str, server_id: int = 0) -> bytes:
    return struct.pack("<HH", 4 + MAX_NAME + 1, MSG_REQUEST_GAMESERVER) + \
        name.encode("ascii", "replace")[:MAX_NAME - 1].ljust(MAX_NAME, b"\0") + \
        bytes([server_id])


def parse_response_gameserver(plain: bytes):
    """Layout differs by compiler padding; try the plausible offsets."""
    body = plain[4:]
    for port_off in (36, 39):
        if len(body) < port_off + 5:
            continue
        ip = body[1:1 + MAX_NAME].split(b"\0")[0]
        port = struct.unpack_from("<I", body, port_off)[0]
        code_off = port_off + 4 + 1
        code = body[code_off:code_off + VALIDATE_CODE_LEN].split(b"\0")[0]
        if 1 <= port <= 65535 and ip and code:
            return ip.decode("ascii", "replace"), port, code.decode("ascii", "replace")
    return None


def cmd_listen(args):
    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind((args.bind, args.port))
    srv.listen(1)
    print(f"[probe] listening on {args.bind}:{args.port}; start the client now", flush=True)
    conn, addr = srv.accept()
    print(f"[probe] connection from {addr}", flush=True)
    cipher = Cipher()
    try:
        while True:
            pkt, _ = recv_packet(conn, cipher, timeout=15)
            if pkt is None:
                print("[probe] connection closed")
                return 1
            size, mtype = pkt["size"], pkt["type"]
            print(f"[probe] packet size={size} type={mtype}", flush=True)
            if mtype == MSG_LOGIN:
                plain = pkt["plain"]
                name = plain[4:4 + MAX_NAME].split(b"\0")[0].decode("ascii", "replace")
                pw = plain[4 + MAX_NAME:4 + 2 * MAX_NAME].split(b"\0")[0].decode("ascii", "replace")
                ver = struct.unpack_from("<i", plain, 4 + 2 * MAX_NAME)[0]
                print(f"[probe] MSG_LOGIN name={name!r} password={pw!r} fVersion={ver}")
                print(f"[probe] ==> set CLIENT_VERSION={ver} in .env and restart the server")
                return 0
            print(f"[probe] raw: {pkt['raw'][:96].hex(' ')}")
    finally:
        conn.close()
        srv.close()


def cmd_test(args):
    print(f"[probe] login server {args.host}:{args.port} account={args.account} version={args.version}")
    s = socket.create_connection((args.host, args.port), timeout=10)
    s = BufferedSock(s)
    s_cipher, r_cipher = Cipher(), Cipher()
    send_packet(s, s_cipher, login_packet(args.account, args.password, args.version))
    pkt, _ = recv_packet(s, r_cipher)
    if pkt is None:
        print("[probe] FAIL: login server closed the connection")
        return 1
    print(f"[probe] login reply: type={pkt['type']} size={pkt['size']}")
    # The 1.0.282 US client shifted the login reply to opcode 3.
    if pkt["type"] not in (MSG_LOGIN_RETURN_INFO, 3) or len(pkt["plain"]) < 5:
        print(f"[probe] FAIL: unexpected reply: {pkt['plain'][:32].hex(' ')}")
        return 1
    info = pkt["plain"][4]
    labels = {0x00: "bad account/name", 0x01: "OK", 0x02: "already logged in",
              0x03: "banned", 0xFF: "VERSION MISMATCH"}
    print(f"[probe] login result 0x{info:02x} ({labels.get(info, 'unknown')})")
    if info != 0x01:
        if info == 0xFF:
            print("[probe] ==> the client/server versions differ; discover the client value "
                  "with --listen and set CLIENT_VERSION")
        return 1

    # The shim pushes the server list right after the login reply
    # (MSG_GAMESERVER_INFO entries), because this client never asks for it.
    # Framing follows whatever this connection negotiated: the probe sends
    # encrypted, so the reply is encrypted too.
    pkt, _ = recv_packet(s, r_cipher, timeout=5)
    if pkt is None:
        print("[probe] FAIL: no server list was pushed")
        return 1
    print(f"[probe] server-list entry type={pkt['type']} size={pkt['size']}")
    if pkt["type"] != 10008 or pkt["size"] < 48:
        print(f"[probe] FAIL: unexpected list entry: {pkt['plain'][:48].hex(' ')}")
        return 1
    name = pkt["plain"][4:12].split(b"\0")[0].decode("ascii", "replace")
    print(f"[probe] server name={name!r}")

    # The pushed entry carries no validate code, so ask the shim for one the way
    # the 1.0.145 client did: send MSG_REQUEST_GAMESERVER (opcode 4 in this
    # build) and read the MSG_RESPONSE_GAMESERVER reply.
    send_packet(s, s_cipher, request_gameserver_packet(args.account, 0))
    pkt, _ = recv_packet(s, r_cipher, timeout=5)
    parsed = parse_response_gameserver(pkt["plain"]) if pkt else None
    if not parsed:
        print("[probe] FAIL: no game-server address/validate code")
        return 1
    ip, port, code = parsed
    print(f"[probe] GameServer {ip}:{port} validation={code!r}")

    gs = socket.create_connection((ip, port), timeout=10)
    g_cipher, gr_cipher = Cipher(), Cipher()
    # sizeof(MSG_LOGIN_GAMESERVER) is 62: 4 + 32 + 25 rounded to the struct's
    # 2-byte alignment. The GameServer frames by Head.usSize, so send the pad.
    body = struct.pack("<HH", 4 + MAX_NAME + VALIDATE_CODE_LEN + 1, args.gs_type) + \
        args.account.encode("ascii", "replace")[:MAX_NAME - 1].ljust(MAX_NAME, b"\0") + \
        code.encode("ascii", "replace").ljust(VALIDATE_CODE_LEN, b"\0") + b"\0"
    if args.gs_frame == "u16":
        body = struct.pack("<H", len(body)) + body
    elif args.gs_frame == "u32":
        body = struct.pack("<I", len(body)) + body
    # IMPORTANT: the client->gameserver direction of this build is PLAINTEXT.
    # Only the login-server link is XOR-encrypted. Sending an encrypted
    # MSG_LOGIN_GAMESERVER makes the GameServer read a garbage header and drop
    # the packet silently, which looks exactly like a validation rejection.
    # Verified against the real client and by direct test: plaintext gets a
    # 356-byte MSG_ROLE_INFO reply, encrypted gets nothing.
    if args.gs_cipher == "plain":
        gs.sendall(body)
        pkt, _ = recv_packet_plain(gs, timeout=15)
    else:
        send_packet(gs, g_cipher, body)
        pkt, _ = recv_packet(gs, gr_cipher, timeout=15)
    if pkt is None:
        print("[probe] FAIL: GameServer closed the connection (validation rejected?)")
        return 1
    print(f"[probe] GameServer replied: type={pkt['type']} size={pkt['size']}"
          + (" (ROLE_INFO)" if pkt["type"] == MSG_ROLE_INFO else ""))
    print("[probe] PASS: login -> server list -> game server handshake all work")
    return 0


def cmd_scan(args):
    """Find the message type the GameServer binary accepts for its login.

    The C++ source is an older revision than the shipped binaries, so the
    numeric ids in Common/MsgTypes.h do not necessarily match the compiled
    dispatch table. For each candidate type: log in, ask the LoginServer for
    the game server (which makes the shim hand a fresh validate code to the
    GameServer), then send a login packet of that type and see if anything
    comes back.
    """
    # One handshake gives one validate code; the GameServer keeps it cached per
    # account, so many candidate login packets can be tried with that code.
    s = socket.create_connection((args.host, args.port), timeout=5)
    s_cipher, r_cipher = Cipher(), Cipher()
    send_packet(s, s_cipher, login_packet(args.account, args.password, args.version))
    pkt, _ = recv_packet(s, r_cipher, timeout=5)
    if not pkt or pkt["type"] != MSG_LOGIN_RETURN_INFO or pkt["plain"][4] != 1:
        print("[probe] login failed; cannot scan")
        return 1
    send_packet(s, s_cipher, request_gameserver_packet(args.account, 0))
    pkt, _ = recv_packet(s, r_cipher, timeout=5)
    parsed = parse_response_gameserver(pkt["plain"]) if pkt else None
    if not parsed:
        print("[probe] no server list; cannot scan")
        return 1
    ip, port, code = parsed
    print(f"[probe] scanning types {args.scan_from}..{args.scan_to} against {ip}:{port}")

    hits = []
    for mtype in range(args.scan_from, args.scan_to + 1):
        try:
            gs = socket.create_connection((ip, port), timeout=3)
            g_cipher, gr_cipher = Cipher(), Cipher()
            body = struct.pack("<HH", 62, mtype) + \
                args.account.encode("ascii", "replace")[:MAX_NAME - 1].ljust(MAX_NAME, b"\0") + \
                code.encode("ascii", "replace")[:VALIDATE_CODE_LEN - 1].ljust(VALIDATE_CODE_LEN, b"\0") + b"\0"
            send_packet(gs, g_cipher, body)
            try:
                gp, _ = recv_packet(gs, gr_cipher, timeout=1.5)
            except OSError:
                gp = None
            gs.close()
            if gp:
                print(f"[probe] type {mtype} -> reply type={gp['type']} size={gp['size']}")
                hits.append(mtype)
        except OSError:
            continue
    if not hits:
        print("[probe] no type produced a reply")
    else:
        print("[probe] accepting types: %s" % hits)
    return 0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--listen", action="store_true", help="act as a fake login server and dump the client's login packet")
    ap.add_argument("--test", action="store_true", help="run a full headless login handshake")
    ap.add_argument("--bind", default="127.0.0.1")
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--port", type=int, default=5999)
    ap.add_argument("--account", default="test")
    ap.add_argument("--password", default="test")
    ap.add_argument("--version", type=int, default=1000270)
    ap.add_argument("--gs-type", type=int, default=10000,
                    help="message type used for the GameServer login")
    ap.add_argument("--gs-frame", default="raw", choices=["raw", "u16", "u32"],
                    help="length prefix the GameServer expects before packets")
    ap.add_argument("--gs-cipher", default="plain", choices=["plain", "xor"],
                    help="client->gameserver framing: plain (correct for this "
                         "build) or xor")
    ap.add_argument("--scan", action="store_true",
                    help="scan GameServer message types for the one it accepts")
    ap.add_argument("--scan-from", type=int, default=1)
    ap.add_argument("--scan-to", type=int, default=120)
    args = ap.parse_args()
    if args.listen:
        return cmd_listen(args)
    if args.scan:
        return cmd_scan(args)
    if args.test:
        return cmd_test(args)
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())

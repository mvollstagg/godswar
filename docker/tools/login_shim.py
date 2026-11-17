#!/usr/bin/env python3
"""Complete the login handshake this LoginServer build never finishes.

Why this exists
---------------
The 2007 LoginServer shipped with the repack answers a login packet with nothing
at all: its version gate was disabled by whoever built it (the startup banner
says "去版本验证") and the only code that ever sends MSG_LOGIN_RETURN_INFO sits
in the now unreachable branch, while the reachable branch just returns. It also
never creates an account session, so the follow-up MSG_REQUEST_GAMESERVER finds
`FindAccount(Name) == NULL`, breaks out silently, and the client waits forever.
The 1.0.145 client did not wait for any of that, which is why it worked; the
1.0.270+ client shows "verifying..." and then a timeout.

This process owns the public login port and does the two steps itself:

  1. MSG_LOGIN                     -> MSG_LOGIN_RETURN_INFO(ucInfo = 1, success)
  2. MSG_REQUEST_GAMESERVER        -> picks a validate code, hands it to the
                                      GameServer (MSG_VALIDATE_GAMESERVER, the
                                      same message the LoginServer would have
                                      sent over the registered gameserver link)
                                      and answers the client with
                                      MSG_RESPONSE_GAMESERVER (IP, port, id,
                                      code). The GameServer caches that code and
                                      accepts the client's MSG_LOGIN_GAMESERVER.

Everything else is passed through to the real LoginServer untouched, including
the GameServer registration. Both directions of this protocol use the same
256-byte XOR keystream from position 0, so pass-through is byte-exact; the shim
only re-encrypts the server->client direction to account for what it injected.

Every packet is logged, which also makes this a protocol capture tool.

Usage:
    python3 login_shim.py [--listen HOST:PORT] [--upstream HOST:PORT]
"""
import argparse
import random
import socket
import string
import struct
import sys
import threading

# <a1, b1, c1, fst1, a2, b2, c2, fst2> from cNet/Encryptor.h (see login_probe.py).
COEFFS = (0xEE, 0x17, 0x33, 0x50, 0x82, 0x23, 0x61, 0x33)

MAX_NAME = 32
VALIDATE_CODE_LEN = 25          # Common/MsgTypes.h VALIDATECODE_LENTH

# Common/MsgTypes.h
MSG_LOGIN = 1
MSG_LOGIN_RETURN_INFO = 2
# The 1.0.282 US client shifted the login-server opcodes: it dispatches its
# login-reply handler on type 3 and sends its server-list request as type 4.
# Disassembly of NetSystemMessage at 0x491502 shows the compare chain
# (cmp eax,0x2710 / je / cmp eax,3 / jne default), and the C# emulator in
# GodsWar-Private-Server-master declares _MSG_REQUEST_GAMESERVER = 4.
MSG_LOGIN_RETURN_INFO_US = 3
MSG_REQUEST_GAMESERVER = 3
MSG_REQUEST_GAMESERVER_US = 4
MSG_VALIDATE_GAMESERVER = 300
MSG_LOGIN_GAMESERVER = 10000
MSG_RESPONSE_GAMESERVER = 10001
MSG_GAMESERVER_INFO = 10008

NAMES = {
    MSG_LOGIN: "MSG_LOGIN",
    MSG_LOGIN_RETURN_INFO: "MSG_LOGIN_RETURN_INFO",
    MSG_LOGIN_RETURN_INFO_US: "MSG_LOGIN_RETURN_INFO(US type 3)",
    MSG_REQUEST_GAMESERVER: "MSG_REQUEST_GAMESERVER",
    MSG_REQUEST_GAMESERVER_US: "MSG_REQUEST_GAMESERVER(US type 4)",
    MSG_VALIDATE_GAMESERVER: "MSG_VALIDATE_GAMESERVER",
    MSG_LOGIN_GAMESERVER: "MSG_LOGIN_GAMESERVER",
    MSG_RESPONSE_GAMESERVER: "MSG_RESPONSE_GAMESERVER",
    MSG_GAMESERVER_INFO: "MSG_GAMESERVER_INFO",
}

CODE_ALPHABET = string.ascii_uppercase + string.digits


def _table(a, b, c, first):
    buf = bytearray(256)
    n = first
    for i in range(256):
        buf[i] = n
        n = (a * n * n + b * n + c) % 256
    return bytes(buf)


def _keystream():
    a1, b1, c1, f1, a2, b2, c2, f2 = COEFFS
    t1 = _table(a1, b1, c1, f1)
    t2 = _table(a2, b2, c2, f2)
    return bytes(x ^ y for x, y in zip(t1, t2))


KS = _keystream()


def log(msg):
    print(msg, flush=True)


def xor_ks(data, pos):
    """XOR data with the keystream starting at absolute position pos."""
    out = bytearray(len(data))
    for i, byte in enumerate(data):
        out[i] = byte ^ KS[(pos + i) % 256]
    return bytes(out)


class Peer:
    """One direction of one connection; tracks its keystream position."""

    def __init__(self, sock, encrypted=True):
        self.sock = sock
        self.pos = 0
        self.encrypted = encrypted

    def send_plain(self, payload):
        if self.encrypted:
            self.sock.sendall(xor_ks(payload, self.pos))
        else:
            self.sock.sendall(payload)
        self.pos += len(payload)


# Types this protocol actually uses (Common/MsgTypes.h) — used to tell an
# encrypted stream from a plaintext one on the first 4 bytes.
KNOWN_TYPES = {1, 2, 3, 4, 300} | set(range(10000, 10013))


def decode_header(raw4, pos, encrypted):
    head = xor_ks(raw4, pos) if encrypted else raw4
    size, mtype = struct.unpack("<HH", head)
    return size, mtype


def detect_mode(raw4, pos):
    """Return True if the stream is encrypted, False if plain, None if unknown."""
    for encrypted in (True, False):
        size, mtype = decode_header(raw4, pos, encrypted)
        if 4 <= size <= 65535 and mtype in KNOWN_TYPES:
            return encrypted
    return None


# The GameServer registers itself on its LoginServer link; remember it so a
# client's MSG_REQUEST_GAMESERVER can be answered with a usable address.
GS = {"peer": None, "lock": None, "ip": "", "port": 0, "id": 0}
GS_MUTEX = threading.Lock()

NO_INJECT = False      # capture-only mode
ADVERTISE_PORT = 0     # port handed to clients instead of the registered one

# Server -> 1.0.282 US client opcode translation.
#
# The US client is a later protocol revision than the 2007 server binaries. It
# inserted messages into the enum, so every receive opcode from MSG_SELFINFO
# onward sits higher than the server's number. Payload layouts are unchanged
# (verified by disassembling the client's handlers and comparing field offsets
# against Common/MsgDefine.h), so only Head.usType needs rewriting.
#
# Derived by matching the client's dispatch-table handler labels against the
# server enum in Common/MsgTypes.h. Opcodes not listed here are already
# identical in both revisions.
from gw_protocol import GameSessionTranslator

OPCODE_MAP = {
    10009: 10019,   # MSG_SELFINFO
    10011: 10021,   # MSG_LEAVE
    10012: 10022,   # MSG_COLONY_LEAVE
    10015: 10018,   # MSG_SCENE_CHANGE
    10021: 10028,   # MSG_UPGRADE
    10022: 10031,   # MSG_KITBAG
    10026: 10037,   # MSG_PARAMATER
    10030: 10041,   # MSG_SELFPROPERTY
    10073: 10094,   # MSG_RELATION_REQUEST
    10074: 10095,   # MSG_RELATION_RESPONSE
    10076: 10097,   # MSG_RELATION
    10117: 10158,   # MSG_MANAGE_RETURN
}
# Standalone mode: this process IS the login server; nothing is forwarded to
# LoginServer.exe. That daemon is not merely silent: it still parses the login
# packet and answers with MSG_LOGIN_RETURN_INFO ucInfo=0 (its account lookup
# never succeeds), so the client received our "success" and then the daemon's
# "not registered" reply, which is what stopped it before it ever asked for
# the game server. Client traffic does not need that daemon at all: the
# GameServer registers on this same port and the shim answers the client.
STANDALONE = False


def pack_str(value, size):
    raw = value.encode("latin-1", "replace")[:size - 1]
    return raw.ljust(size, b"\0")


def _reply_variant(version=0):
    """Which MSG_LOGIN_RETURN_INFO shape to send.

    Two client generations speak two different login protocols:

      * 1.0.145 and older (fVersion < 1000200): opcode 2, 6-byte reply, then the
        client asks for the game server itself with MSG_REQUEST_GAMESERVER (3)
        and expects MSG_RESPONSE_GAMESERVER (10001).
      * 1.0.282 US: opcode 3, and the server list is pushed inline as
        MSG_GAMESERVER_INFO (10008) entries because this build never asks.

    Override with /tmp/gw_login_reply when experimenting.
    """
    try:
        with open("/tmp/gw_login_reply", "r") as fh:
            forced = fh.read().strip()
            if forced:
                return forced
    except OSError:
        pass
    return "t3list" if version >= 1000200 else "v6"


def server_list_packet(ip, port, name="GodsWar", server_id=1):
    """MSG_LOGIN_RETURN_INFO(ucInfo=1) followed by one MSG_GAMESERVER_INFO entry.

    The 1.0.282 client does not use MSG_RESPONSE_GAMESERVER for the server
    picker: it expects the list as MSG_GAMESERVER_INFO (type 10008) entries
    after the login reply, and it never asks for them.

    Entry layout is 78 bytes, read straight out of the client's 10008 handler
    (NetSystemMessage, dispatch index for 10008 -> 0x4918cc). The handler holds
    the packet at object offset 4, so its reads map to wire offsets:

        [ebx+0x24] -> wire  36   IP string
        [ebx+0x44] -> wire  68   port (dword)
        [ebx+0x4c] -> wire  76   server id (byte, must be < 1000)
        [ebx+0x4d] -> wire  77   state (byte, 0..3)

    and the name sits at wire 4 (32 bytes). The 48-byte retail capture in the
    emulator is an older, smaller variant: feeding it to this build makes the
    name read as garbage and the UI report "no server available".
    """
    head = struct.pack("<HH", 6, MSG_LOGIN_RETURN_INFO_US) + bytes([1]) + b"\0"

    body = bytearray(78 - 4)
    body[0:32] = pack_str(name, 32)                    # wire 4  -> name
    body[32:64] = pack_str(ip, 32)                     # wire 36 -> ip
    struct.pack_into("<I", body, 64, port)             # wire 68 -> port
    body[72] = server_id & 0xFF                        # wire 76 -> id
    body[73] = 1                                       # wire 77 -> state (idle)

    entry = struct.pack("<HH", 4 + len(body), MSG_GAMESERVER_INFO) + bytes(body)
    assert len(entry) == 78, len(entry)
    return head + entry


def login_ok_packet(variant="v6"):
    # MSG_LOGIN_RETURN_INFO { MsgHead; BYTE ucInfo }  (the legacy size is 6)
    #
    # NOTE the type: the 1.0.282 US client dispatches its login-reply handler on
    # type 3, not 2. Disassembly of NetSystemMessage at 0x491502 shows
    # `movzx eax,[ebx+6] / cmp eax,0x2710 / je ... / cmp eax,3 / jne <default>`,
    # so this build inserted an opcode after _MSG_LOGIN and everything below
    # 10000 shifted up by one. Sending type 2 makes the client drop the packet
    # as unknown and sit on "Verifying..." until it times out.
    mtype = 3 if variant.startswith("t3") else MSG_LOGIN_RETURN_INFO
    if variant in ("t3", "t3v6", "t3push", "t3list"):
        return struct.pack("<HH", 6, mtype) + bytes([1]) + b"\0"
    if variant == "v5":
        return struct.pack("<HH", 5, mtype) + bytes([1])
    if variant == "v8":
        return struct.pack("<HH", 8, mtype) + bytes([1]) + b"\0" * 3
    if variant == "big":
        # Head + ucInfo + 67 zero bytes, in case this build frames a larger
        # struct than the 2007 one did.
        body = bytes([1]) + b"\0" * 67
        return struct.pack("<HH", 4 + len(body), mtype) + body
    if variant == "echo140":
        # Mirror the client's 140-byte login size with ucInfo=1 up front.
        body = bytes([1]) + b"\0" * 135
        return struct.pack("<HH", 4 + len(body), mtype) + body
    return struct.pack("<HH", 6, mtype) + bytes([1]) + b"\0"


def validate_packet(account, code):
    # MSG_VALIDATE_GAMESERVER { MsgHead; char Accounts[32]; char Code[25] }
    # MSVC pads the struct to 62 bytes and the GameServer frames packets by
    # Head.usSize, so the pad byte has to be sent too (61 stalls its reader).
    body = pack_str(account, MAX_NAME) + pack_str(code, VALIDATE_CODE_LEN)
    body += b"\0" * (-len(body) % 2)
    return struct.pack("<HH", 4 + len(body), MSG_VALIDATE_GAMESERVER) + body


def response_packet(server_id, ip, port, code, error=0):
    # MSG_RESPONSE_GAMESERVER { Head; BYTE id; char cIP[32]; UINT uiPort;
    #                           BYTE cLoginError; char cCheckOutText[25] }
    # MSVC layout: ip at 5, uiPort 4-aligned at 40, code at 45, sizeof 72.
    body = bytearray(68)
    body[0] = server_id & 0xFF
    body[1:1 + MAX_NAME] = pack_str(ip, MAX_NAME)
    struct.pack_into("<I", body, 36, port)          # 40 - 4 (head)
    body[40] = error & 0xFF                          # 44 - 4
    body[41:41 + VALIDATE_CODE_LEN] = pack_str(code, VALIDATE_CODE_LEN)
    return struct.pack("<HH", 4 + len(body), MSG_RESPONSE_GAMESERVER) + bytes(body)


def make_code():
    return "".join(random.choice(CODE_ALPHABET) for _ in range(VALIDATE_CODE_LEN - 1))


def translate_server_to_client(plain):
    """Rewrite server opcodes to the 1.0.282 US client's numbering.

    The US client is a later protocol revision: it inserted messages, so its
    receive opcodes sit higher than the 2007 server's. Payload layouts are
    unchanged (verified by disassembling the client's handlers and comparing
    field offsets against Common/MsgDefine.h), so only Head.usType is rewritten.

    Only the game-server stream needs this. The login shim answers the client
    itself and already speaks the right numbering, so NO_INJECT (the game shim
    runs with --no-inject) is what enables translation.

    Returns (rewritten_bytes, [(from, to), ...]) for logging.
    """
    if not OPCODE_MAP or not NO_INJECT:
        return plain, []
    out = bytearray(plain)
    changes = []
    pos = 0
    while pos + 4 <= len(out):
        size, mtype = struct.unpack_from("<HH", out, pos)
        if size < 4 or size > 65535 or pos + size > len(out):
            break
        new = OPCODE_MAP.get(mtype)
        if new is not None:
            struct.pack_into("<H", out, pos + 2, new)
            changes.append((mtype, new))
        pos += size
    return bytes(out), changes


def handle(client, upstream_addr):
    upstream = None
    if not STANDALONE:
        try:
            upstream = socket.create_connection(upstream_addr, timeout=15)
        except OSError as exc:
            log(f"[shim] upstream {upstream_addr} unreachable: {exc}")
            client.close()
            return

    lock = threading.Lock()
    to_client = Peer(client)      # what we write back to the accepted peer
    answered = threading.Event()
    mode = {"encrypted": None}     # discovered from the first packet
    mode_known = threading.Event()

    translator = GameSessionTranslator(log_fn=log) if NO_INJECT else None
    if translator is not None:
        # 1.0.282 GameServer links are plaintext; unblock server_to_client immediately.
        mode["encrypted"] = False
        to_client.encrypted = False
        mode_known.set()

    def client_to_server():
        buf = b""
        pos = 0
        try:
            while True:
                data = client.recv(4096)
                if not data:
                    log("[shim] client closed the connection")
                    break
                if translator is not None:
                    srv_bytes, cli_reply, notes = translator.client_to_server_chunk(data)
                    with lock:
                        if srv_bytes and upstream is not None:
                            upstream.sendall(srv_bytes)
                        if cli_reply:
                            to_client.sock.sendall(cli_reply)
                    if notes:
                        log(f"[shim] c2s: {', '.join(notes)}")
                    continue
                # Both sides start at 0 and this direction is never rewritten.
                if upstream is not None:
                    with lock:
                        upstream.sendall(data)
                log(f"[shim] <- client {len(data)}B {data[:64].hex(' ')}")
                if len(data) > 64:
                    log(f"[shim] <- client FULL {len(data)}B {data.hex(' ')}")
                buf += data
                while len(buf) >= 4:
                    if mode["encrypted"] is None:
                        guess = detect_mode(buf[:4], pos)
                        if guess is None:
                            log(f"[shim] undecodable stream, raw={buf[:16].hex(' ')}")
                            buf = b""
                            break
                        mode["encrypted"] = guess
                        to_client.encrypted = guess
                        mode_known.set()
                        log(f"[shim] link is {'encrypted' if guess else 'plaintext'}")
                    size, mtype = decode_header(buf[:4], pos, mode["encrypted"])
                    if size < 4 or size > 65535:
                        log(f"[shim] desync (size={size}); dropping buffer")
                        buf = b""
                        break
                    if len(buf) < size:
                        break
                    header = (xor_ks(buf[:size], pos) if mode["encrypted"]
                              else buf[:size])
                    pos += size
                    buf = buf[size:]
                    log(f"[shim] -> server {NAMES.get(mtype, mtype)} size={size}")

                    if NO_INJECT:
                        continue

                    if mtype == MSG_LOGIN:
                        acct = header[4:4 + MAX_NAME].split(b"\0")[0].decode("latin-1")
                        ver = 0
                        if size >= 4 + 2 * MAX_NAME + 4:
                            ver = struct.unpack_from("<i", header, 4 + 2 * MAX_NAME)[0]
                        # The 1.0.282 US client sends a 140-byte login with
                        # fVersion in the last 4 bytes, not at 4+2*32.
                        if size >= 140:
                            ver = struct.unpack_from("<i", header, size - 4)[0]
                        variant = _reply_variant(ver)
                        log(f"[shim] login account={acct!r} fVersion={ver} "
                            f"size={size} reply={variant} -> success")
                        with lock:
                            if variant == "enc":
                                to_client.encrypted = True
                            if variant == "t3list":
                                # Push the server picker contents inline: the
                                # 1.0.282 client never asks for them.
                                with GS_MUTEX:
                                    snap = dict(GS)
                                gs_name = snap.get("name") or "GodsWar"
                                gs_ip = snap.get("ip") or "127.0.0.1"
                                to_client.send_plain(server_list_packet(
                                    gs_ip,
                                    ADVERTISE_PORT or 6001,
                                    "GodsWar", snap.get("id") or 1))
                                log(f"[shim] sent server list for {acct!r} "
                                    f"({gs_name})")
                            else:
                                to_client.send_plain(login_ok_packet(variant))
                            # The 1.0.282 client never sends its own
                            # MSG_REQUEST_GAMESERVER: it shows the server-select
                            # screen and waits for the list to be pushed. Send
                            # it right after the login reply.
                            if variant.endswith("push"):
                                with GS_MUTEX:
                                    snap = dict(GS)
                                if snap["ip"] and snap["port"]:
                                    code = make_code()
                                    try:
                                        with snap["lock"]:
                                            snap["peer"].send_plain(
                                                validate_packet(acct, code))
                                    except OSError as exc:
                                        log(f"[shim] gameserver link dead: {exc}")
                                    public_port = ADVERTISE_PORT or snap["port"]
                                    to_client.send_plain(response_packet(
                                        snap["id"], snap["ip"], public_port, code))
                                    log(f"[shim] pushed server list for {acct!r}: "
                                        f"{snap['ip']}:{public_port} code={code}")
                                else:
                                    log("[shim] push requested but no gameserver")
                        answered.set()

                    elif mtype == MSG_GAMESERVER_INFO and size >= 76:
                        ip = header[4:36].split(b"\0")[0].decode("latin-1")
                        port = struct.unpack_from("<I", header, 36)[0]
                        with GS_MUTEX:
                            GS.update(peer=to_client, lock=lock, ip=ip,
                                      port=port, id=header[40])
                        log(f"[shim] gameserver registered: {ip}:{port} id={header[40]}")
                    elif mtype in (MSG_REQUEST_GAMESERVER, MSG_REQUEST_GAMESERVER_US):
                        acct = header[4:4 + MAX_NAME].split(b"\0")[0].decode("latin-1")
                        with GS_MUTEX:
                            snap = dict(GS)
                        if snap["peer"] is None or not snap["ip"] or not snap["port"]:
                            log("[shim] request gameserver: none registered")
                            with lock:
                                to_client.send_plain(
                                    response_packet(0, "", 0, "", error=1))
                            continue
                        code = make_code()
                        try:
                            with snap["lock"]:
                                snap["peer"].send_plain(validate_packet(acct, code))
                            log(f"[shim] validate code handed to {snap['ip']}:{snap['port']}")
                        except OSError as exc:
                            log(f"[shim] gameserver link dead: {exc}")
                        public_port = ADVERTISE_PORT or snap["port"]
                        with lock:
                            to_client.send_plain(response_packet(
                                snap["id"], snap["ip"], public_port, code))
                        log(f"[shim] gameserver for {acct!r}: "
                            f"{snap['ip']}:{ADVERTISE_PORT or snap['port']} code={code}")
        except OSError:
            pass
        finally:
            with GS_MUTEX:
                if GS["peer"] is to_client:
                    GS.update(peer=None, lock=None, ip="", port=0, id=0)
                    log("[shim] gameserver link closed")
            for sock in (client, upstream):
                if sock is None:
                    continue
                try:
                    sock.shutdown(socket.SHUT_RDWR)
                except OSError:
                    pass
                sock.close()

    def server_to_client():
        if upstream is None:
            return
        total = 0
        mode_known.wait(timeout=3)
        encrypted = mode["encrypted"]
        if encrypted is None:
            encrypted = True
        sbuf = b""
        logged = 0
        try:
            while True:
                data = upstream.recv(4096)
                if not data:
                    log("[shim] server closed the connection")
                    break
                log(f"[shim] <- {len(data)}B {data[:64].hex(' ')}")
                with lock:
                    if encrypted:
                        # Decrypt at the server's send position, re-encrypt at
                        # ours (which already accounts for our injections).
                        plain = xor_ks(data, total)
                        total += len(data)
                        plain, changes = translate_server_to_client(plain)
                        for old, new in changes:
                            log(f"[shim] retype {NAMES.get(old, old)} "
                                f"{old} -> {new}")
                        to_client.sock.sendall(xor_ks(plain, to_client.pos))
                    else:
                        if translator is not None:
                            plain, changes = translator.server_to_client_chunk(data)
                        else:
                            plain, changes = translate_server_to_client(data)
                        total += len(data)
                        for old, new in changes:
                            log(f"[shim] retype {NAMES.get(old, old)} "
                                f"{old} -> {new}")
                        if plain:
                            to_client.sock.sendall(plain)
                    # Log whole packets in the server's stream for capture.
                    # sbuf holds bytes not yet logged; its first byte sits at
                    # offset `logged` in the decrypted server stream.
                    sbuf += plain
                    while len(sbuf) >= 4:
                        psize, ptype = decode_header(sbuf[:4], logged, encrypted)
                        if psize < 4 or psize > 65535 or len(sbuf) < psize:
                            break
                        log(f"[shim] <- server {NAMES.get(ptype, ptype)} "
                            f"size={psize}")
                        sbuf = sbuf[psize:]
                        logged += psize
                    to_client.pos += len(plain)
        except OSError:
            pass

    t = threading.Thread(target=server_to_client, daemon=True)
    t.start()
    client_to_server()
    t.join(timeout=5)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--listen", default="0.0.0.0:5999")
    ap.add_argument("--upstream", default="127.0.0.1:5998")
    ap.add_argument("--no-inject", action="store_true",
                    help="pure pass-through with packet logging (for capture)")
    ap.add_argument("--standalone", action="store_true",
                    help="answer every client message here; never talk to "
                         "the upstream LoginServer")
    ap.add_argument("--advertise-port", type=int, default=0,
                    help="port to tell clients instead of the registered one")
    args = ap.parse_args()
    global NO_INJECT, ADVERTISE_PORT, STANDALONE
    NO_INJECT = args.no_inject
    ADVERTISE_PORT = args.advertise_port
    STANDALONE = args.standalone

    lhost, lport = args.listen.rsplit(":", 1)
    uhost, uport = args.upstream.rsplit(":", 1)

    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind((lhost, int(lport)))
    srv.listen(64)
    log(f"[shim] listening on {args.listen} -> "
        f"{'standalone' if STANDALONE else args.upstream}")

    while True:
        try:
            client, addr = srv.accept()
        except OSError:
            continue
        log(f"[shim] connection from {addr[0]}:{addr[1]}")
        threading.Thread(
            target=handle, args=(client, (uhost, int(uport))), daemon=True
        ).start()


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        sys.exit(0)

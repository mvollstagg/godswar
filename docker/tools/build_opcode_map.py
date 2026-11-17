#!/usr/bin/env python3
"""Derive the server->client opcode map from the client's own dispatch table.

The client's handlers are identified by their debug label (the first string the
handler pushes). Those labels are the client's names for the messages. Match
them against the server's enum names, tolerating the client's extra suffixes
("MSG_LEAVE  ID=%d" -> MSG_LEAVE).
"""
import re
import struct

# ---------------- server enum ----------------
src = open("/tmp/MsgTypes.h", "r", encoding="latin-1").read()
lines = src.splitlines()
start = next(i for i, l in enumerate(lines) if "_MSG_INVALID" in l and "=" in l)
body_lines = []
for line in lines[start:]:
    if line.strip().startswith("}"):
        break
    body_lines.append(line)

server = {}
value = -1
for raw in "\n".join(body_lines).split(","):
    line = raw.split("//")[0].strip()
    if not line:
        continue
    if "=" in line:
        name, val = line.split("=", 1)
        name = name.strip()
        try:
            value = int(val.strip(), 0)
        except ValueError:
            continue
    else:
        name = line.strip()
        value += 1
    if not name:
        continue
    server[name.lstrip("_")] = value

# ---------------- client dispatch ----------------
data = open("/tmp/GodsWar.exe", "rb").read()
pe_off = struct.unpack_from("<I", data, 0x3C)[0]
nsec = struct.unpack_from("<H", data, pe_off + 6)[0]
opt_size = struct.unpack_from("<H", data, pe_off + 20)[0]
opt_off = pe_off + 24
image_base = struct.unpack_from("<I", data, opt_off + 28)[0]
sec_off = opt_off + opt_size

sections = []
for i in range(nsec):
    o = sec_off + i * 40
    nm = data[o:o + 8].rstrip(b"\0").decode("latin-1")
    vsize, vaddr, rsize, raddr = struct.unpack_from("<IIII", data, o + 8)
    sections.append((nm, vaddr, vsize, raddr, rsize))


def va_to_off(va):
    rva = va - image_base
    for nm, vaddr, vsize, raddr, rsize in sections:
        if vaddr <= rva < vaddr + max(vsize, rsize):
            return raddr + (rva - vaddr)
    return None


def cstr(va):
    off = va_to_off(va)
    if off is None:
        return None
    end = data.find(b"\0", off)
    if end < 0 or end - off > 80:
        return None
    raw = data[off:end]
    if not raw or any(b < 32 or b > 126 for b in raw):
        return None
    return raw.decode("latin-1")


BYTE_TAB = 0x495554
DWORD_TAB = 0x49548C
DEFAULT = 0x00491D0D

client = {}
for mtype in range(10000, 10200):
    idx = mtype - 0x2711
    off = va_to_off(BYTE_TAB + idx)
    if off is None:
        continue
    b = data[off]
    target = struct.unpack_from("<I", data, va_to_off(DWORD_TAB + b * 4))[0]
    if target == DEFAULT:
        continue
    hoff = va_to_off(target)
    if hoff is None:
        continue
    label = ""
    for k in range(0, 96):
        if data[hoff + k] == 0x68:
            imm = struct.unpack_from("<I", data, hoff + k + 1)[0]
            s = cstr(imm)
            if s and len(s) > 2:
                label = s
                break
    if label:
        client[mtype] = label

# ---------------- match ----------------
print(f"{'server':>7} {'client':>7}  name")
mapping = {}
for cnum, label in sorted(client.items()):
    token = label.split()[0].lstrip("_")
    if token in server:
        s = server[token]
        mapping[s] = cnum
        flag = "" if s == cnum else "   <-- SHIFTED"
        print(f"{s:>7} {cnum:>7}  {token}{flag}")

print("\nOPCODE_MAP = {")
for s in sorted(mapping):
    if mapping[s] != s:
        print(f"    {s}: {mapping[s]},")
print("}")

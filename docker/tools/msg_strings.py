#!/usr/bin/env python3
"""Dump the client's MSG_* name strings in file order (enum declaration order)."""
import re
import struct

PATH = "/tmp/GodsWar.exe"
data = open(PATH, "rb").read()

pe_off = struct.unpack_from("<I", data, 0x3C)[0]
nsec = struct.unpack_from("<H", data, pe_off + 6)[0]
opt_size = struct.unpack_from("<H", data, pe_off + 20)[0]
opt_off = pe_off + 24
image_base = struct.unpack_from("<I", data, opt_off + 28)[0]
sec_off = opt_off + opt_size

sections = []
for i in range(nsec):
    o = sec_off + i * 40
    name = data[o:o + 8].rstrip(b"\0").decode("latin-1")
    vsize, vaddr, rsize, raddr = struct.unpack_from("<IIII", data, o + 8)
    sections.append((name, vaddr, vsize, raddr, rsize))

# Walk .rdata collecting printable null-terminated strings in order.
for name, vaddr, vsize, raddr, rsize in sections:
    if name != ".rdata":
        continue
    blob = data[raddr:raddr + rsize]
    pos = 0
    while pos < len(blob):
        end = blob.find(b"\0", pos)
        if end < 0:
            break
        raw = blob[pos:end]
        if 4 <= len(raw) <= 40 and all(32 <= b < 127 for b in raw):
            s = raw.decode("latin-1")
            if re.fullmatch(r"_?MSG_[A-Z0-9_]+", s):
                va = image_base + vaddr + pos
                print(f"{va:#010x}  {s}")
        pos = end + 1

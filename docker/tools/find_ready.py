#!/usr/bin/env python3
"""Identify client handlers by their debug strings, and find the CLIENT_READY send."""
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


def va_to_off(va):
    rva = va - image_base
    for name, vaddr, vsize, raddr, rsize in sections:
        if vaddr <= rva < vaddr + max(vsize, rsize):
            return raddr + (rva - vaddr)
    return None


def cstr(va):
    off = va_to_off(va)
    if off is None:
        return "<unmapped>"
    end = data.find(b"\0", off)
    return data[off:end].decode("latin-1", "replace")


print("=== handler debug strings ===")
for va in (0x7cf610, 0x7cf628, 0x7cf63c, 0x7cf650, 0x7cf660, 0x7cf670):
    print(f"{va:#010x}: {cstr(va)!r}")

print("\n=== code referencing 0x2717 (10007 CLIENT_READY) ===")
text = next(s for s in sections if s[0] == ".text")
_, tvaddr, tvsize, traddr, trsize = text
text_bytes = data[traddr:traddr + trsize]

# mov WORD PTR [reg+disp], 0x2717  -> 66 c7 4x xx 17 27
# push 0x2717                      -> 68 17 27 00 00
patterns = {
    "push 0x2717": b"\x68\x17\x27\x00\x00",
    "mov word [reg+6],0x2717": b"\x66\xc7\x40\x06\x17\x27",
    "mov word [reg+4],0x2717": b"\x66\xc7\x40\x04\x17\x27",
}
for label, pat in patterns.items():
    start = 0
    hits = []
    while True:
        j = text_bytes.find(pat, start)
        if j < 0:
            break
        hits.append(image_base + tvaddr + j)
        start = j + 1
    print(f"{label}: {len(hits)} hit(s)")
    for h in hits[:8]:
        print(f"   {h:#010x}")

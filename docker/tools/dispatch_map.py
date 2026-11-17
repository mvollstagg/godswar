#!/usr/bin/env python3
"""Dump the client's NetSystemMessage dispatch table for the 10000+ range."""
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


BYTE_TAB = 0x495554
DWORD_TAB = 0x49548C
DEFAULT = 0x00491D0D

for mtype in range(10000, 10060):
    idx = mtype - 0x2711
    off = va_to_off(BYTE_TAB + idx)
    b = data[off]
    target = struct.unpack_from("<I", data, va_to_off(DWORD_TAB + b * 4))[0]
    tag = "  <-- default/unknown" if target == DEFAULT else ""
    print(f"{mtype}: handler={target:#010x}{tag}")

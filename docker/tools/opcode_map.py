#!/usr/bin/env python3
"""Build the client's real opcode -> handler-name map.

For each 10000+ opcode, follow the dispatch to its handler and pull the first
C string the handler pushes (its debug label). That label is the client's own
name for the message, which is what we need to align with the server.
"""
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

print(f"{'opcode':>6}  {'handler':>10}  label")
for mtype in range(10000, 10060):
    idx = mtype - 0x2711
    off = va_to_off(BYTE_TAB + idx)
    if off is None:
        continue
    b = data[off]
    target = struct.unpack_from("<I", data, va_to_off(DWORD_TAB + b * 4))[0]
    if target == DEFAULT:
        continue
    # First `push imm32` in the handler that points at a readable string.
    label = ""
    hoff = va_to_off(target)
    for k in range(0, 96):
        if data[hoff + k] == 0x68:  # push imm32
            imm = struct.unpack_from("<I", data, hoff + k + 1)[0]
            s = cstr(imm)
            if s and len(s) > 2:
                label = s
                break
    print(f"{mtype:>6}  {target:#010x}  {label}")

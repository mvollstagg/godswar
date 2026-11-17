#!/usr/bin/env python3
"""Print the server's message enum values >= 10000, in declaration order."""
import re

src = open("/tmp/MsgTypes.h", "r", encoding="latin-1").read()
lines = src.splitlines()

start = next(i for i, l in enumerate(lines) if "_MSG_INVALID" in l and "=" in l)

value = -1
rows = []
for line in lines[start:]:
    stripped = line.strip()
    if stripped.startswith("};"):
        break
    code = line.split("//")[0]
    # A declaration may be "NAME = 5," or "NAME," possibly several per line.
    for piece in code.split(","):
        piece = piece.strip()
        if not piece:
            continue
        m = re.match(r"([A-Za-z_]\w*)\s*(?:=\s*(.+))?$", piece)
        if not m:
            continue
        name, val = m.group(1), m.group(2)
        if val is not None:
            try:
                value = int(val.strip(), 0)
            except ValueError:
                continue
        else:
            value += 1
        rows.append((value, name))

for v, n in rows:
    if v >= 10000:
        print(f"{v}\t{n.lstrip('_')}")

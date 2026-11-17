#!/usr/bin/env python3
"""Parse the server's MsgTypes.h enum into number -> name."""
import re
import sys

PATH = "/mnt/msgt/MsgTypes.h"
src = open(PATH, "r", encoding="latin-1").read()

# Grab the enum body.
m = re.search(r"enum\s*\w*\s*\{(.*?)\}", src, re.S)
body = m.group(1)

value = -1
out = []
for raw in body.split(","):
    line = raw.split("//")[0].strip()
    if not line:
        continue
    if "=" in line:
        name, val = line.split("=", 1)
        name = name.strip()
        val = val.strip()
        try:
            value = int(val, 0)
        except ValueError:
            continue
    else:
        name = line.strip()
        value += 1
    if not name:
        continue
    out.append((value, name))

for v, n in out:
    if v >= 10000:
        print(f"{v}\t{n}")

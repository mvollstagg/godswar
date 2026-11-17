#!/usr/bin/env python3
"""Add missing NPC appearance sections to the client's Settings/Sys/NPC.ini.

The server's scene files name every NPC it spawns; the client looks that name up
as a section in Settings/Sys/NPC.ini to know which avatar parts to draw. The
bundled 1.0.145 client predates the Athens_Newbie scene data, so its NPC.ini has
no section for the English ids used there and the client stops with:

    section[Athens_Newbie_013_MaleHero2] not found in ./Settings/Sys/NPC.ini

This tool finds every server NPC name that the client is missing and appends a
section cloned from an existing entry of the same sex (appearance is generic,
the avatar part ids are guaranteed to exist because they come from the client's
own file). The original file is backed up once as NPC.INI.orig.

Usage:
    python docker/tools/patch-client-npc.py [--client Client] [--scenes ".../Scene"]
"""
import argparse
import os
import sys

DEFAULT_SCENES = os.path.join(
    "GW 2.0", "Server Files (Compiled)", "gameserver", "Scene"
)

AVATAR_KEYS = [
    "avatar_body", "avatar_boots", "avatar_gloves", "avatar_hair", "avatar_head",
    "avatar_cap", "avatar_circle", "avatar_leggins", "avatar_sleeves",
    "avatar_twohand", "avatar_bow", "avatar_mainhand", "avatar_shield",
    "avatar_assistant",
]


def read_text(path):
    """Read byte-exact: the file is GB2312 and must not be re-encoded."""
    with open(path, "rb") as fh:
        return fh.read().decode("latin-1")


def parse_sections(text):
    sections = []
    current = None
    for raw in text.splitlines():
        line = raw.rstrip("\r")
        stripped = line.strip()
        if stripped.startswith("[") and stripped.endswith("]"):
            current = (stripped[1:-1], [])
            sections.append(current)
        elif current is not None:
            current[1].append(line)
    return sections


def get_field(lines, key):
    for line in lines:
        if "=" not in line:
            continue
        name, _, value = line.partition("=")
        if name.strip().lower() == key:
            return value.strip()
    return None


def server_npc_names(scenes_dir):
    """Map every server NPC name to the Script id it runs (may be empty)."""
    names = {}
    for root, _dirs, files in os.walk(scenes_dir):
        for fname in files:
            if not fname.upper().startswith("NPC") or not fname.upper().endswith(".INI"):
                continue
            name = script = None
            for line in read_text(os.path.join(root, fname)).splitlines():
                line = line.strip()
                low = line.lower()
                if low.startswith("name="):
                    name = line.split("=", 1)[1].strip()
                elif low.startswith("script="):
                    script = line.split("=", 1)[1].strip()
                if name is not None and script is not None:
                    if name:
                        names[name] = script
                    name = script = None
            if name:
                names[name] = ""
    return names


def id_prefix(name):
    """'Athens_001_AthenianCivilian1' -> 'Athens_001_', '墨勒提欧_雅典男文官1' -> None.

    The server names NPCs two ways: the newer scenes (Athens_Newbie) use the
    client's own section id, the older ones (Athens, Sparta …) use a
    '<chinese name>_<chinese avatar tag>' pair. Both keep the scene/NPC id in
    the matching Script name, so the client's English section can be found by
    the '<scene>_<nnn>_' prefix taken from the Script path instead.
    """
    parts = name.split("_")
    for index, part in enumerate(parts):
        if part.isdigit():
            return "_".join(parts[: index + 1]) + "_"
    return None


def pick_templates(sections):
    """Return (male_lines, female_lines) cloned from the client's own entries."""
    male = female = None
    for header, lines in sections:
        if get_field(lines, "avatar_body") is None:
            continue
        sex = get_field(lines, "sex")
        if sex == "1" and male is None:
            male = lines
        elif sex == "0" and female is None:
            female = lines
        if male and female:
            break
    return male or female, female or male


def build_section(name, template, female_hint):
    sex = "0" if female_hint else "1"
    out = ["[%s]" % name, "name             =%s" % name, "sex              =%s" % sex]
    for key in AVATAR_KEYS:
        value = get_field(template, key)
        if value is not None:
            out.append("%-17s=%s" % (key, value))
    out.append("%-17s=%s" % ("Range", get_field(template, "Range") or "1.0f"))
    out.append("%-17s=%s" % ("Shadow", get_field(template, "Shadow") or "1.5f"))
    return out


def find_npc_ini(client_dir):
    """Locate NPC.INI in either client layout.

    1.0.145 keeps its data at the client root (Settings/Sys/NPC.INI); later
    clients (1.0.262/1.0.270) keep per-locale data under
    Localization/<locale>/Settings/Sys/NPC.INI.
    """
    direct = os.path.join(client_dir, "Settings", "Sys", "NPC.INI")
    if os.path.isfile(direct):
        return direct
    locales = os.path.join(client_dir, "Localization")
    if os.path.isdir(locales):
        for name in sorted(os.listdir(locales)):
            candidate = os.path.join(locales, name, "Settings", "Sys", "NPC.INI")
            if os.path.isfile(candidate):
                return candidate
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--client", default="Client")
    ap.add_argument("--scenes", default=DEFAULT_SCENES)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    npc_ini = find_npc_ini(args.client)
    if not npc_ini:
        print("client NPC ini not found under: %s" % args.client)
        return 1
    if not os.path.isdir(args.scenes):
        print("scene directory not found: %s" % args.scenes)
        return 1

    text = read_text(npc_ini)
    sections = parse_sections(text)
    have = {header.strip().lower() for header, _ in sections}
    wanted = server_npc_names(args.scenes)
    missing = [n for n in sorted(wanted) if n.strip().lower() not in have]

    # English-keyed sections of the client, indexed by '<scene>_<nnn>_'.
    by_prefix = {}
    for header, lines in sections:
        prefix = id_prefix(header.strip())
        if prefix and get_field(lines, "avatar_body") is not None:
            by_prefix.setdefault(prefix, lines)

    print("client sections: %d | server npc names: %d | missing: %d"
          % (len(sections), len(wanted), len(missing)))
    if not missing:
        print("nothing to do")
        return 0

    male_tpl, female_tpl = pick_templates(sections)
    if male_tpl is None:
        print("no usable template section in client NPC.ini")
        return 1

    new_blocks = []
    exact = generic = 0
    for name in missing:
        script = wanted.get(name, "")
        npc_id = script.rsplit(".", 1)[-1] if script else ""
        template = by_prefix.get(npc_id + "_") if npc_id else None
        if template is not None:
            # Same NPC, other naming scheme: keep its own appearance.
            exact += 1
            female_hint = get_field(template, "sex") == "0"
        else:
            generic += 1
            female_hint = "fem" in name.lower()
            template = female_tpl if (female_hint and female_tpl) else male_tpl
        new_blocks.append(build_section(name, template, female_hint))
        print("  + %s (%s%s)" % (name, "female" if female_hint else "male",
                                 "" if template is not None and npc_id and by_prefix.get(npc_id + "_") is not None else ", generic"))
    print("matched client section by script id: %d | generic fallback: %d"
          % (exact, generic))

    if args.dry_run:
        print("dry run: nothing written")
        return 0

    backup = npc_ini + ".orig"
    if not os.path.exists(backup):
        with open(backup, "wb") as fh:
            fh.write(text.encode("latin-1"))
        print("backup written: %s" % backup)

    addition = []
    for block in new_blocks:
        addition.append("")
        addition.extend(block)
    appended = text
    if not appended.endswith("\n"):
        appended += "\r\n"
    appended += "\r\n".join(addition) + "\r\n"

    with open(npc_ini, "wb") as fh:
        fh.write(appended.encode("latin-1"))
    print("appended %d sections to %s" % (len(new_blocks), npc_ini))
    print("restart the client (it reads Settings at startup)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

# GodsWar Online legacy server (2007 C++ / VC9) under Wine.
# All server binaries are 32-bit Windows PEs, so this image provides a
# 32-bit Wine prefix plus the pieces needed for headless supervision.
FROM --platform=linux/amd64 debian:bookworm-slim

ENV DEBIAN_FRONTEND=noninteractive \
    WINEPREFIX=/wine \
    WINEARCH=win32 \
    WINEDEBUG=-all \
    # winedbg.exe=d disables the crash debugger: on an unhandled exception the
    # process then terminates instead of leaving an interactive winedbg hanging
    # (which made the daemon look alive to the supervisor forever).
    WINEDLLOVERRIDES="mscoree,mshtml=;winedbg.exe=d" \
    DISPLAY=:99 \
    LANG=C.UTF-8 \
    TZ=Europe/Istanbul

RUN dpkg --add-architecture i386 \
 && apt-get update \
 && apt-get install -y --no-install-recommends \
        wine wine32:i386 \
        xvfb socat iproute2 netcat-openbsd procps tini ca-certificates \
        binutils python3 \
 && rm -rf /var/lib/apt/lists/*

# Create the prefix at build time and make Wine behave like Windows 7: these
# 2005-era winsock/IOCP servers are known to misbehave on newer Windows.
RUN wineboot --init >/dev/null 2>&1 || true ; \
    wineserver -w || true ; \
    wine reg add 'HKCU\Software\Wine' /v Version /d win7 /f >/dev/null 2>&1 || true ; \
    wineserver -w || true ; \
    # No crash dialog / no auto debugger either way (belt and braces with the
    # winedbg override above); a fault must end the process, not block it.
    wine reg add 'HKCU\Software\Wine\WineDbg' /v ShowCrashDialog /t REG_DWORD /d 0 /f >/dev/null 2>&1 || true ; \
    wine reg add 'HKLM\Software\Microsoft\Windows NT\CurrentVersion\AeDebug' /v Auto /t REG_SZ /d 0 /f >/dev/null 2>&1 || true ; \
    wineserver -w || true

# The server embeds Python 2.5 (python25.dll) for scripts/ and Script/*.py.
# Between the scripts and the game there are C extensions: Script/Altar.py does
# `import math`, which only exists in a real CPython 2.5 installation (DLLs/*.pyd).
# On the original Windows host that came from a system-wide C:\Python25. Without
# it the import returns NULL and GameServer.exe dereferences it (page fault at
# 0x1E0903F5, right after the "Initialize ChatHandler" progress bar).
COPY docker/python25/runtime /wine/drive_c/Python25
ENV PYTHONHOME=C:\\Python25

# The server tree lives in a folder whose name contains spaces, which the
# Dockerfile parser cannot quote reliably; compose passes it as the named
# build context `gw` instead.
COPY --from=gw . /opt/gw/

RUN mkdir -p /opt/gw/logs \
 && chmod -R a+rX /opt/gw

COPY docker/server/entrypoint.sh /usr/local/bin/entrypoint.sh
COPY docker/tools/login_shim.py /usr/local/bin/login_shim.py
COPY docker/tools/gw_protocol.py /usr/local/bin/gw_protocol.py
COPY docker/tools/gw_npc_map.py /usr/local/bin/gw_npc_map.py
# Strip CRLF in case the repo was checked out with core.autocrlf=true on Windows.
RUN sed -i 's/\r$//' /usr/local/bin/entrypoint.sh /usr/local/bin/login_shim.py /usr/local/bin/gw_protocol.py /usr/local/bin/gw_npc_map.py \
 && chmod +x /usr/local/bin/entrypoint.sh /usr/local/bin/login_shim.py

WORKDIR /opt/gw
ENTRYPOINT ["/usr/bin/tini", "--", "/usr/local/bin/entrypoint.sh"]

@echo off
rem Launch GodsWar directly against the server in Client\config.ini.
rem Patcher.exe / Launch.exe are skipped: they would try to reach the retail
rem update server instead of the local one.
cd /d "%~dp0"
start "" "GodsWar.exe"

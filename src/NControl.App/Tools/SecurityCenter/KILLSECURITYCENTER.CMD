@ECHO OFF
SETLOCAL EnableExtensions

IF /I NOT "%~1"=="Disable" EXIT /B 64

REM Run by SuperUser as TrustedInstaller. Disable tamper protection first,
REM then hand off to the full Defender/Security Center removal routine.
REG ADD "HKLM\SOFTWARE\Microsoft\Windows Defender\Features" /F /V "TamperProtection" /T REG_DWORD /D 4 >NUL 2>&1
IF ERRORLEVEL 1 EXIT /B 65

CALL "%~dp0DEFENDER.CMD" Disable
REM DEFENDER.CMD inherits the exit code of its final cosmetic command. Enforce
REM the security service/driver start values here and let NControl verify them.
FOR %%S IN (WinDefend wscsvc WdNisSvc wdnisdrv Sense WdBoot WdFilter SecurityHealthService SgrmAgent SgrmBroker) DO (
    REG QUERY "HKLM\SYSTEM\CurrentControlSet\Services\%%S" >NUL 2>&1 && REG ADD "HKLM\SYSTEM\CurrentControlSet\Services\%%S" /F /V "Start" /T REG_DWORD /D 4 >NUL 2>&1
)

EXIT /B 0

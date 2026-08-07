@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION
if /i "%~1#"=="#" Exit
TITLE %~1 Windows Defender......

IF NOT Exist "%WinDir%\SysWOW64" (SET "Y=\") ELSE (SET "Y=\ \WOW6432Node\")
FOR /F "Tokens=4 Delims= " %%A IN ('CMD /C VER') DO (FOR /F "Tokens=1-3 Delims=." %%X IN ('ECHO %%A') DO (FOR /F "Tokens=1 Delims=]" %%W IN ('ECHO %%Z') DO (SET /a WVE=%%X%%Y%%W)))
SET "CI0={2781761E-28E0-4109-99FE-B9D127C57AFE}"
SET "CI1={56FFCC30-D398-11D0-B2AE-00A0C908FA49}"
SET "CI2={2781761E-28E2-4109-99FE-B9D127C57AFE}"
SET "T1=Microsoft\" 
SET "T2=Policies\%T1%"
SET "T3=HKLM\Software"
SET "T4=HKU\.DEFAULT\Software"
SET "ST8=Edge\ExemptDomainFileTypePairsFromFileTypeDownloadWarnings"
SET "ST7=%T3%\%T2%"
SET "ST6=\shellex\ContextMenuHandlers\EPP"
SET "ST5=%T3%\Classes"
SET "ST4=HKLM\SYSTEM\CurrentControlSet\Services\"
SET "ST3=Security Center\Provider\Av\{D68DDC3A-831F-4fae-9E44-DA132C1ACF46}"
SET "ST2=%T2%Windows Defender\UX Configuration"
SET "ST1=%T3%\%T1%"
SET "ST0=%ST1%Windows\CurrentVersion\"
SET /A ND=0
FOR %%I IN (^"Cache Maintenance^" Cleanup ^"Scheduled Scan^" Verification) DO (
	SCHTASKS /Query /TN "\Microsoft\Windows\Windows Defender\Windows Defender %%~I"
	IF !ErrorLevel! GTR 0 (SCHTASKS /Create /TN "\Microsoft\Windows\Windows Defender\Windows Defender %%~I" /xml "%~dp0Windows Defender %%~I.xml")
	SCHTASKS /Change /TN "\Microsoft\Windows\Windows Defender\Windows Defender %%~I" /%~1
)
cls
CALL :%~1
EXIT /b


:Disable
REM A 禁用（或不可用）

REM A1 关闭相关服务，设定启动参数
FOR %%A IN (WinDefend wscsvc WdNisSvc wdnisdrv Sense WdBoot WdFilter SecurityHealthService SgrmAgent SgrmBroker) DO (
	SC Query %%A >nul 2>nul
    IF !errorlevel! NEQ 1060 (
	   SC Stop %%A >NUL 2>NUL
	   REG Add "%ST4%%%A" /f /v "Start" /t REG_DWORD /d 4 >NUL 2>NUL
))

REM A2 关闭运行中的相关程序
FOR %%A IN (SecurityHealthService SecurityHealthHost SecurityHealthSystray MsMpEng SmartScreen NisSrv MsSense) DO (TASKLIST|Find /I "%%A.exe" >NUL 2>NUL && TaskKill /f /im %%A.exe >NUL 2>NUL)

REM A3 去启动自动运行
REG Delete "%ST0%Explorer\StartupApproved\Run" /f /v "SecurityHealth" >NUL 2>NUL
REG Delete "%ST0%Run" /f /v "WindowsDefender" >NUL 2>NUL
REG Delete "%ST0%Run" /f /v "SecurityHealth" >NUL 2>NUL

REM A4 设定注册表
REG Add "%ST4%WdBoot" /f /v "Group" /t REG_SZ /d "nothing" >NUL 2>NUL
FOR %%A IN (EnableWebContentEvaluation PreventOverride) DO (REG Add "%ST0%AppHost" /f /v "%%A" /t REG_DWORD /d 0 >NUL 2>NUL
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost" /f /v "%%A" /t REG_DWORD /d 0 >NUL 2>NUL)

REG Delete "%ST5%\*%ST6%" /f >NUL 2>NUL
FOR %%A IN (Directory Drive) DO REG Delete "%ST5%\%%A%ST6%" /f >NUL 2>NUL
REG Add "%ST1%%ST3%" /f /v "STATE" /t REG_DWORD /d 393472 >NUL 2>NUL
REG Add "%ST1%Security Center" /f /v "cval" /t REG_DWORD /d 0 >NUL 2>NUL
REG Delete "%ST1%AMSI\UacProviders\%CI2%" /f >NUL 2>NUL
FOR %%I IN (%Y%) DO (
	SET "CSS=%T3%%%I%T1%" & SET "CST=%T3%%%I%T2%"
	SET "USS=%T4%%%I%T1%" & SET "UST=%T4%%%I%T2%"
	FOR %%J IN (Spyware Virus) DO REG Add "!CST!Windows Defender" /f /v "DisableAnti%%J" /t REG_DWORD /d 1 >NUL 2>NUL
	FOR %%J IN (Notification_Suppress SuppressRebootNotification) DO REG Add "!CST!Windows Defender\UX Configuration" /f /v "%%J" /t REG_DWORD /d 1 >NUL 2>NUL
	FOR %%J IN (\ 2\) DO REG Delete "!CSS!AMSI\Providers%%J%CI0%" /f >NUL 2>NUL
	REG Delete "%ST5%%%ICLSID\%CI0%\Implemented Categories\%CI1%" /f >NUL 2>NUL
	REG Delete "!CSS!Internet Explorer\Extension Validation\%CI0%" /f >NUL 2>NUL
	
	SET /A N=0
 	FOR %%J IN (exe inf) DO (SET /A N+=1
		FOR %%K IN (%T2% %T2%Microsoft) DO (REG Add "%T3%%%I%%K%ST8%" /f /v "!N!" /t REG_SZ /d "{\"file_extension\": \"%%J\", \"domains\": [\"*\"]}" >NUL 2>NUL
))
	FOR %%L IN (!CSS! !CST! !USS! !UST!) DO (
		FOR %%K IN (Edge) DO (
			FOR %%J IN (E PuaE) DO REG Add "%%L%%~K" /f /v "SmartScreen%%Jnabled" /t REG_DWORD /d 0 >NUL 2>NUL
			FOR %%J IN (SmartScreenPromptOverrideForFiles) DO REG Add "%%L%%~K" /f /v "Prevent%%J" /t REG_DWORD /d 0 >NUL 2>NUL
)))

REG Add "%ST7%Windows Defender\SmartScreen" /f /v "ConfigureAppInstallControlEnabled" /t REG_DWORD /d 0 >NUL 2>NUL
FOR %%I IN (%ST7%Windows\System %ST0%Explorer) DO REG Add "%%I" /f /v "EnableSmartScreen" /t REG_DWORD /d 0 >NUL 2>NUL
REG Add "%ST0%Explorer" /f /v "SmartScreenEnabled" /t REG_SZ /d "Off" >NUL 2>NUL
REG Add "%T3%\%T2%Windows Defender" /f /v "DisableRoutinelyTakingAction" /t REG_DWORD /d 1 >NUL 2>NUL
FOR %%I IN (BehaviorMonitoring IOAVProtection OnAccessProtection RealtimeMonitoring) DO (REG Add "%T3%\%T2%Windows Defender\Real-Time Protection" /f /v "Disable%%I" /t REG_DWORD /d 1 >NUL 2>NUL
)
TASKLIST|Find /I "explorer.exe" && Call :RExplorer
GOTO :EOF

:Enable
REM B 允许 （或可用）
FOR %%I IN (BehaviorMonitoring IOAVProtection OnAccessProtection RealtimeMonitoring) DO (Reg delete "%T3%\%T2%Windows Defender\Real-Time Protection" /f /v "Disable%%I" >NUL 2>NUL)
Reg delete "%T3%\%T2%Windows Defender" /f /v "DisableRoutinelyTakingAction" >NUL 2>NUL
REM B1 设定注册表
REG Add "%ST0%Explorer" /f /v "SmartScreenEnabled" /t REG_SZ /d "Warn" >NUL 2>NUL
FOR %%I IN (%ST7%Windows\System %ST0%Explorer) DO REG Delete "%%I" /f /v "EnableSmartScreen" >NUL 2>NUL
REG Delete "%ST7%Windows Defender\SmartScreen" /f /v "ConfigureAppInstallControlEnabled" >NUL 2>NUL
SET "CIE={09A47860-11B0-4DA5-AFA5-26D86198A780}"
FOR %%I IN (%Y%) DO (
	SET "CSS=%T3%%%I%T1%" & SET "CST=%T3%%%I%T2%"
	SET "USS=%T4%%%I%T1%" & SET "UST=%T4%%%I%T2%"
	FOR %%L IN (!CSS! !CST! !USS! !UST!) DO (
		FOR %%K IN (Edge) DO (
			FOR %%J IN (PreventSmartScreenPromptOverrideForFiles) DO REG Delete "%%L%%~K" /f /v "%%J" >NUL 2>NUL
			FOR %%J IN (E PuaE) DO REG Delete "%%L%%~K" /f /v "SmartScreen%%Jnabled" >NUL 2>NUL
))
	FOR %%J IN (1 2) DO (REG Delete "%T3%%%I%T2%%ST8%" /f /v "%%J" >NUL 2>NUL
		REG Delete "%T3%%%I%T2%Microsoft%ST8%" /f /v "%%J" >NUL 2>NUL)
	REG Add "!CSS!Internet Explorer\Extension Validation\%CI0%" /f >NUL 2>NUL
	REG Add "%ST5%%%ICLSID\%CI0%\Implemented Categories\%CI1%" /f >NUL 2>NUL
	REG Add "!CSS!AMSI\Providers\%CI0%" /f >NUL 2>NUL
	IF %WVE% GEQ 10021996 (REG Add "!CSS!AMSI\Providers2\%CI0%" /f >NUL 2>NUL)
	REG Delete "!CST!Windows Defender\UX Configuration" /f >NUL 2>NUL
	FOR %%J IN (Spyware Virus) DO REG Delete "!CST!Windows Defender" /f /v "DisableAnti%%J" >NUL 2>NUL
)
REG Add "%ST1%AMSI\UacProviders\%CI2%" /f >NUL 2>NUL
REG Add "%ST1%Security Center" /f /v "cval" /t REG_DWORD /d 1 >NUL 2>NUL
REG Add "%ST1%%ST3%" /f /v "STATE" /t REG_DWORD /d 397568 >NUL 2>NUL
FOR %%I IN (Directory Drive) DO REG Add "%ST5%\%%I%ST6%" /f /ve /t REG_SZ /d "!CIE!" >NUL 2>NUL
REG Add "%ST5%\*%ST6%" /f /ve /t REG_SZ /d "!CIE!" >NUL 2>NUL
FOR %%A IN (EnableWebContentEvaluation PreventOverride) DO (REG Add "%ST0%AppHost" /f /v "%%A" /t REG_DWORD /d 1 >NUL 2>NUL
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost" /f /v "%%A" /t REG_DWORD /d 1 >NUL 2>NUL)
REG Add "%ST4%WdBoot" /f /v "Group" /t REG_SZ /d "Early-Launch" >NUL 2>NUL

REM B2 设定启动自动运行
REG Add "%ST0%Run" /f /v "SecurityHealth" /t REG_EXPAND_SZ /d "%%WinDir%%\system32\SecurityHealthSystray.exe" >NUL 2>NUL
REG Add "%ST0%Explorer\StartupApproved\Run" /f /v "SecurityHealth" /t REG_BINARY /d 040000000000000000000000

REM B3 设定相关服务参数并启动
CALL :SERF "WdBoot WdFilter SgrmAgent" "boot" 0
CALL :SERF "WinDefend wscsvc SgrmBroker" "auto" 2
IF %WVE% LSS 10017763 (CALL :SERF SecurityHealthService "auto" 2) ELSE (CALL :SERF SecurityHealthService "demand" 3)
CALL :SERF "wdnisdrv WdNisSvc Sense" "demand" 3

REM B4 立即启动相关组件
For %%y IN (WinDefend WdNisSvc Sense) DO (For /F "skip=1 Delims=" %%A IN ('REG Query %ST4%%%y /v ImagePath') DO (For /F Tokens^=2^ Delims^=^" %%i IN ('Echo %%A') DO (IF EXIST "%%i" "%%i")))
SecurityHealthService.exe
SmartScreen.exe
START /B SecurityHealthSystray.exe
SET /A ND+=1
TIMEOUT /T 1 >NUL 2>NUL
IF !ND! LSS 30 (
	TASKLIST|Find /I "SecurityHealthSystray.exe" && (IF !ND! LSS 2 GOTO :Enable) || (GOTO :Enable)
)
GOTO :EOF


:SERF

REM 备忘：
REM SC config “SERNER_NAME” START= {BOOT|SYSTEM|AUTO|DEMAND|DISABLED|delayed-auto}
REM Start=0 boot 		由启动引导程序加载的设备驱动器。
REM Start=1 system 		在核心初始化过程中启动的设备驱动器。
REM Start=2 auto（自动）	每次计算机重新启动时，甚至没有人登录计算机时，都能自动开始的服务。
REM Start=3 demand（手动）	必须手动启动的服务。如果没有指定“start=”，这就默认值。
REM Start=4 DISABLED（禁用）	不能启动的服务。要启动一个禁用的服务，把启动类型更改为其他值
REM delayed-auto 		自动延时启动。此时start=2并且DelayedAutostart=1 ；Start为其它值时，DelayedAutostart为1不起作用。
REM 				服务管理器不能及时更新delayedautostart的值。

FOR %%A IN (%~1) DO (
	SC Query %%A >nul 2>nul
    IF !errorlevel! NEQ 1060 (
	  REG Add "%ST4%%%A" /f /v "Start" /t REG_DWORD /d %~3 >NUL 2>NUL
	  if %~3 LSS 3 (SC Start %%A >NUL 2>NUL)
))
GOTO :EOF


:RExplorer
FOR %%A IN (PastIconsStream IconStreams) DO REG Delete "HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify" /f /v "%%A" >NUL 2>NUL
IF %WVE% LSS 10021996 (Taskkill /f /im explorer.exe >NUL 2>NUL
Start explorer.exe)
GOTO :EOF
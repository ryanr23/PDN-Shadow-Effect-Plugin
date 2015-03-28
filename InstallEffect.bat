@echo off

SET TargetPath=%1

FOR /F "tokens=3,4" %%a in ('%Windir%/sysnative/reg query HKLM\SOFTWARE\paint.net /v TARGETDIR') do SET PaintDotNetInstallDir=%%a %%b

IF NOT EXIST "%PaintDotNetInstallDir%" GOTO PDN_NOT_INSTALLED

echo Copying %TargetPath% to "%PaintDotNetInstallDir%\Effects" ...

IF EXIST "%TargetPath%" copy %TargetPath% "%PaintDotNetInstallDir%\Effects" /y
exit /B 0

:PDN_NOT_INSTALLED
echo ERROR: Paint.NET not installed
exit /B 0


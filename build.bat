@echo off
echo Try to find the highest version of MSBuild available...
set MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set MSBUILD_SCRIPT="TurbofilmVpn.sln"
set POST_BUILD_COMMAND=
if not exist %MSBUILD% (
	echo MSBuild not found, please update %0
	pause
	exit /b 1
)

set MSBUILD_ARGUMENTS=%MSBUILD_ARGUMENTS% /p:Configuration=Release

echo MSBUILD: %MSBUILD%
echo MSBUILD_SCRIPT: %MSBUILD_SCRIPT%
echo MSBUILD_ARGUMENTS: %MSBUILD_ARGUMENTS%
%MSBUILD% /nologo /fl %MSBUILD_SCRIPT% %MSBUILD_ARGUMENTS%
%POST_BUILD_COMMAND%
exit /b 0

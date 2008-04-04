rem @ECHO OFF

rem Goto advancedBuild

"%ProgramFiles%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" /rebuild Release "IR Server Suite.sln"
"%ProgramFiles%\NSIS\makensis.exe" setup\setup.nsi

EXIT

:advancedBuild

setup\DeployVersionSVN.exe /svn="%CD%"

"%ProgramFiles%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" /rebuild Release "IR Server Suite.sln"

setup\DeployVersionSVN.exe /svn="%CD%" /revert

rem these commands are necessary to get the svn revision, to enable them just remove the EXIT one line above
setup\DeployVersionSVN.exe /svn="%CD%" /GetVersion
IF NOT EXIST version.txt EXIT
SET /p version=<version.txt
DEL version.txt
"%ProgramFiles%\NSIS\makensis.exe" /DVER_BUILD=%version% setup\setup.nsi
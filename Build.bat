@ECHO OFF

SET DEVENV="C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\devenv.com"
SET SOLUTION="NeoEdit.sln"
SET PLATFORM="Release|x64"

svn up --non-interactive
%DEVENV% %SOLUTION% /clean %PLATFORM%
if exist Build.log del Build.log
%DEVENV% %SOLUTION% /build %PLATFORM% /out Build.log

IF %ERRORLEVEL% == 0 GOTO SUCCESS

ECHO.
ECHO.
ECHO Errors occured.
PAUSE

:SUCCESS

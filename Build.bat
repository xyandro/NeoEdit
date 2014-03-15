@ECHO OFF

SET DEVENV=C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\devenv.com
SET SOLUTION=NeoEdit.sln
SET CONFIGURATION=Release
SET PLATFORM=x64

SET BUILDDIR=bin\%CONFIGURATION%.%PLATFORM%
RD /S /Q %BUILDDIR%

svn up --non-interactive
"%DEVENV%" "%SOLUTION%" /clean "%CONFIGURATION%|%PLATFORM%"
IF EXIST Build.log DEL Build.log
"%DEVENV%" "%SOLUTION%" /build "%CONFIGURATION%|%PLATFORM%" /out Build.log

IF %ERRORLEVEL% == 0 GOTO SUCCESS

ECHO.
ECHO.
ECHO Errors occured.
PAUSE
GOTO EXIT

:SUCCESS
DEL %BUILDDIR%\*.pdb
DEL %BUILDDIR%\*.metagen
DEL %BUILDDIR%\*.config
DEL %BUILDDIR%\*.ilk
ECHO.
ECHO.
ECHO Success!

:EXIT

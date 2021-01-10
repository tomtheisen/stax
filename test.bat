@echo off

@REM for /f "usebackq tokens=*" %%i in (`vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
@REM   "%%i" /nologo /verbosity:m /p:Configuration=Release
@REM )

@REM echo ========================
@REM echo .NET test
@REM echo ========================
@REM StaxLang.CLI\bin\Release\stax.exe -tests testspecs

pushd StaxLang.JS

  echo.
  echo ========================
  echo Node test
  echo ========================
  REM call is needed here.  without it the script terminates after this line
  call npm run --silent test

  @REM echo.
  @REM call npm run --silent test -- --nobigint

popd
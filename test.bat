@echo off

REM msbuild /nologo /verbosity:m
REM msbuild only works in VS prompt because of PATH :(
echo ========================
echo .NET test
echo ========================
StaxLang.CLI\bin\Debug\stax.exe -tests testspecs

cd StaxLang.JS
echo.
echo ========================
echo Node test
echo ========================
npm run --silent test
cd ..

@echo off
REM Version Increment for Visual Studio Copyright (C) 2021  daniznf  github.com/daniznf/versionincrement
REM This program is free software: you can redistribute it and/or modify
REM     it under the terms of the GNU General Public License as published by
REM     the Free Software Foundation, either version 3 of the License, or
REM     (at your option) any later version.

REM This program is distributed in the hope that it will be useful,
REM     but WITHOUT ANY WARRANTY; without even the implied warranty of
REM     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
REM     GNU General Public License for more details.

REM You should have received a copy of the GNU General Public License
REM     along with this program.  If not, see <https://www.gnu.org/licenses/>.



echo Version Increment for Visual Studio
echo Version 1.1.4.0 - August 2021
echo Author daniznf
echo.
goto thestart

:usage
echo Use this script to automatically increment Build Number and or Revision in Visual Studio projects
echo In Visual Studio Pre-Build event command line put a line like this:
echo.
echo $(ProjectDir)Properties\VersionIncrement.bat $(ProjectDir) /B /R
echo.
echo Or you can call this file directly by a command prompt:
echo.
echo VersionIncrement.bat projectDir /B ^| /R
echo.
echo /B paramenter increments Build Number
echo /R paramenter increments Revision
echo.
echo Please specify any or both /B and /R
echo.

pause.
goto theend

REM Use this script to automatically increment Build Number and or Revision in Visual Studio projects
REM In Visual Studio Pre-Build event command line put a line like this:
REM
REM $(ProjectDir)Properties\VersionIncrement.bat $(ProjectDir) /B /R
REM
REM Or you can call this file directly by a command prompt
REM VersionIncrement.bat projectDir /B | /R
REM
REM /B paramenter increments Build Number
REM /R paramenter increments Revision
REM Please specify any or both /B and /R

REM ###################################################################
REM You can change following values according to your needs

:thestart
set assemblyFile=%1\Properties\AssemblyInfo.cs
set assemblyFileBackup=%1\Properties\AssemblyInfo.cs.bkp

REM Until here
REM ###################################################################

set incrementBuild=no
set incrementRevision=no
if "%1"=="/B" set incrementBuild=yes
if "%2"=="/B" set incrementBuild=yes
if "%3"=="/B" set incrementBuild=yes
if "%1"=="/R" set incrementRevision=yes
if "%2"=="/R" set incrementRevision=yes
if "%3"=="/R" set incrementRevision=yes

set inParams=no
if %incrementBuild%==yes set inParams=yes
if %incrementRevision%==yes set inParams=yes
if %inParams%==no goto usage

if not exist %assemblyFile% goto AssemblyFileNotFound
copy /Y /V %assemblyFile% %assemblyFileBackup%

findstr /V /R "Assembly.*Version" %assemblyFileBackup% > %assemblyFile%
findstr /R "Assembly.*Version" %assemblyFileBackup% | findstr "\//" >> %assemblyFile%

REM AssemblyVersion
for /f "tokens=1,2,3 delims=()" %%a in ('findstr AssemblyVersion %assemblyFileBackup%') do set openingAVersion=%%~a(
for /f "tokens=1,2,3 delims=()" %%a in ('findstr AssemblyVersion %assemblyFileBackup%') do set aVersion=%%~b
for /f "tokens=1,2,3 delims=()" %%a in ('findstr AssemblyVersion %assemblyFileBackup%') do set closingAVersion=)%%~c

for /f "tokens=1,2,3,4 delims=." %%a in ("%aVersion%") do set aMajor=%%a
for /f "tokens=1,2,3,4 delims=." %%a in ("%aVersion%") do set aMinor=%%b
for /f "tokens=1,2,3,4 delims=." %%a in ("%aVersion%") do set aBuild=%%c
for /f "tokens=1,2,3,4 delims=." %%a in ("%aVersion%") do set aRevision=%%d

if %incrementBuild%==yes set /a aBuild=%aBuild%+1
if %incrementRevision%==yes set /a aRevision=%aRevision%+1
set aVersion=%aMajor%.%aMinor%.%aBuild%.%aRevision%
echo New AssemblyVersion %aVersion%

echo %openingAVersion%"%aVersion%"%closingAVersion%>> %assemblyFile%

REM AssemblyFileVersion
for /f "tokens=1,2,3 delims=()" %%a in ('findstr AssemblyFileVersion %assemblyFileBackup%') do set openingAFVersion=%%~a(
for /f "tokens=1,2,3 delims=()" %%a in ('findstr AssemblyFileVersion %assemblyFileBackup%') do set aFVersion=%%~b
for /f "tokens=1,2,3 delims=()" %%a in ('findstr AssemblyFileVersion %assemblyFileBackup%') do set closingAFVersion=)%%~c

for /f "tokens=1,2,3,4 delims=." %%a in ("%aFVersion%") do set aFMajor=%%a
for /f "tokens=1,2,3,4 delims=." %%a in ("%aFVersion%") do set aFMinor=%%b
for /f "tokens=1,2,3,4 delims=." %%a in ("%aFVersion%") do set aFBuild=%%c
for /f "tokens=1,2,3,4 delims=." %%a in ("%aFVersion%") do set aFRevision=%%d

if %incrementBuild%==yes set /a aFBuild=%aFBuild%+1
if %incrementRevision%==yes set /a aFRevision=%aFRevision%+1
set aFVersion=%aFMajor%.%aFMinor%.%aFBuild%.%aFRevision%
echo New AssemblyFileVersion %aFVersion%

echo %openingAFVersion%"%aFVersion%"%closingAFVersion%>> %assemblyFile%

goto cleanEnd

:AssemblyFileNotFound
Echo Error, AssemblyInfo.cs not found
goto theend

:cleanEnd

:theend

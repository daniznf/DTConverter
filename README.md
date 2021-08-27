# Version Increment
Increments AssemblyVersion and or AssemblyFileVersion of projects in Visual Studio

Use this script to automatically increment Build Number and or Revision in Visual Studio projects
In Visual Studio Pre-Build event command line put a line like this:

> $(ProjectDir)Properties\VersionIncrement.bat $(ProjectDir) /B /R

Or you can call this file directly by a command prompt:

` VersionIncrement.bat projectDir /B | /R  ` 

* /B paramenter increments Build Number
* /R paramenter increments Revision

Please specify any or both /B and /R

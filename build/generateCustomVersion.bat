@echo off
cls

echo Write the source branch name (you current branch name, where were the changes made): 
set /p userDefinedSourceBranch=
echo.
echo Write the destination branch name (the branch from which you can get the latest changes, like 'develop' or 'main/master'): 
set /p userDefinedDestinationBranch=
echo.
echo Write your custom version number: 
set /p userDefinedVersion=
echo.

:: Set init params value
set applicationName=SoapClientCallAssist
set runVersionIncrement=y
set runGenChangeLog=y
:: If runBuild > y(yes), build in release mode
set runBuild=y
set runSolutionTest=n
set runTest=y
set runPack=y
set assemblyPath=$('..\src\shared\GeneralAssemblyInfo.cs')
set genType=-2
set setInChangeLogNewVersion=y
set autoCommitAndPush=n
set autoGetLatestDevelop=y
set changeLogPath=$('..\docs\CHANGELOG.md')
set sourceBranch=%userDefinedSourceBranch%
set destinationBranch=%userDefinedDestinationBranch%
set customVersion=%userDefinedVersion%
set solutionPath=$('..\src\RzR.Shared.Services.sln')
set packResultPath=$('..\nuget\')
set packProjectsPath=$('..\src\SoapClientCallAssist\SoapClientCallAssist.csproj')
set testProjectsPath=$('..\src\tests\SoapClientCallAssistTests.Soap12\SoapClientCallAssistTests.Soap12.csproj','..\src\tests\SoapClientCallAssistTests.Wcf\SoapClientCallAssistTests.Wcf.csproj','..\src\tests\SoapClientCallAssistTests\SoapClientCallAssistTests.csproj')


echo :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
echo :::         Initialize:                                           :::
echo :::            - New application version generation               :::
echo :::            - Change log generation                            :::
echo :::            - Build                                            :::
echo :::            - Test                                             :::
echo :::            - Create package                                   :::
echo :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
echo:
echo:

PowerShell -NoProfile -ExecutionPolicy ByPass -Command ".\GenerateBuildInfo.exe -scriptCommands \"runVersionIncrement=%runVersionIncrement%;runGenChangeLog=%runGenChangeLog%;runBuild=%runBuild%;runSolutionTest=%runSolutionTest%;runTest=%runTest%;runPack=%runPack%;setInChangeLogNewVersion=%setInChangeLogNewVersion%;autoCommitAndPush=%autoCommitAndPush%;autoGetLatestDevelop=%autoGetLatestDevelop%;changeLogPath=%changeLogPath%;sourceBranch=%sourceBranch%;destinationBranch=%destinationBranch%;assemblyPath=%assemblyPath%;customVersion=%customVersion%;genType=%genType%;solutionPath=%solutionPath%;packResultPath=%packResultPath%;packProjectsPath=%packProjectsPath%;testProjectsPath=%testProjectsPath%\"";

echo.
pause

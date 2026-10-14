param(
    [string]$GameDirectory = '',
    [string]$AnchorChainDirectory = '',
    [switch]$Test,
    [switch]$InstallLocal
)
$ErrorActionPreference = 'Stop'
$taskDotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (-not (Test-Path $taskDotnet)) { throw 'Install a .NET SDK before building.' }
$taskProperties = @()
if ($GameDirectory) { $taskProperties += "-p:SeaPowerDir=$GameDirectory" }
if ($AnchorChainDirectory) { $taskProperties += "-p:AnchorChainDir=$AnchorChainDirectory" }
function Invoke-TaskDotnet([string[]]$TaskArguments) {
    $taskQuoted = $TaskArguments | ForEach-Object { '"' + $_ + '"' }
    $taskStartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $taskStartInfo.FileName = $taskDotnet
    $taskStartInfo.Arguments = $taskQuoted -join ' '
    $taskStartInfo.UseShellExecute = $false
    $taskStartInfo.CreateNoWindow = $true
    $taskStartInfo.RedirectStandardOutput = $true
    $taskStartInfo.RedirectStandardError = $true
    $taskStartInfo.StandardOutputEncoding = New-Object System.Text.UTF8Encoding($false)
    $taskStartInfo.StandardErrorEncoding = New-Object System.Text.UTF8Encoding($false)
    # Wait for the command, not persistent compiler-server child processes.
    $taskProcess = [System.Diagnostics.Process]::Start($taskStartInfo)
    $taskOutput = $taskProcess.StandardOutput.ReadToEndAsync()
    $taskErrors = $taskProcess.StandardError.ReadToEndAsync()
    $taskProcess.WaitForExit()
    if ($taskOutput.Result) { Write-Output $taskOutput.Result.TrimEnd() }
    if ($taskErrors.Result) { Write-Output $taskErrors.Result.TrimEnd() }
    if ($taskProcess.ExitCode -ne 0) { throw "dotnet failed ($($taskProcess.ExitCode))." }
}
$taskProject = Join-Path $PSScriptRoot 'src\NoesisGlyphCacheExpansion.csproj'
Invoke-TaskDotnet (@('build', $taskProject, '-c', 'Release', '--nologo', '--verbosity', 'minimal') + $taskProperties)
if ($Test) {
    $taskTestProject = Join-Path $PSScriptRoot 'tests\NoesisGlyphCacheExpansion.Tests.csproj'
    Invoke-TaskDotnet (@('build', $taskTestProject, '-c', 'Release', '--nologo', '--verbosity', 'minimal') + $taskProperties)
    Invoke-TaskDotnet @((Join-Path $PSScriptRoot 'tests\bin\Release\net10.0\NoesisGlyphCacheExpansion.Tests.dll'))
}
$taskDll = Join-Path $PSScriptRoot 'src\bin\Release\netstandard2.1\NoesisGlyphCacheExpansion.dll'
$taskPackage = Join-Path $PSScriptRoot 'dist\NoesisGlyphCacheExpansion'
New-Item -ItemType Directory -Force $taskPackage | Out-Null
Copy-Item $taskDll (Join-Path $taskPackage 'NoesisGlyphCacheExpansion.dll') -Force
Copy-Item (Join-Path $PSScriptRoot 'package\_info.ini') (Join-Path $taskPackage '_info.ini') -Force
Copy-Item (Join-Path $PSScriptRoot 'README.md') (Join-Path $taskPackage 'README.md') -Force
Write-Output "Workshop package: $taskPackage"
if ($InstallLocal) {
    if (-not $GameDirectory) {
        $taskLocalProperties = Join-Path $PSScriptRoot 'Directory.Build.local.props'
        if (-not (Test-Path $taskLocalProperties)) { throw 'Use -GameDirectory for local installation.' }
        [xml]$taskXml = Get-Content $taskLocalProperties
        $GameDirectory = [string]$taskXml.Project.PropertyGroup.SeaPowerDir
    }
    if (-not (Test-Path (Join-Path $GameDirectory 'Sea Power.exe'))) { throw 'Sea Power.exe not found in GameDirectory.' }
    $taskDestination = Join-Path $GameDirectory 'Sea Power_Data\StreamingAssets\NoesisGlyphCacheExpansion'
    New-Item -ItemType Directory -Force $taskDestination | Out-Null
    foreach ($taskFile in Get-ChildItem $taskPackage -File) {
        $taskInstalled = Join-Path $taskDestination $taskFile.Name
        if ($taskFile.Extension -eq '.dll' -and (Test-Path $taskInstalled)) {
            $taskTemporary = $taskInstalled + '.installing'
            Copy-Item $taskFile.FullName $taskTemporary -Force
            [System.IO.File]::Replace($taskTemporary, $taskInstalled, [NullString]::Value)
        } else {
            Copy-Item $taskFile.FullName $taskInstalled -Force
        }
    }
    Write-Output "Installed local mod: $taskDestination"
}

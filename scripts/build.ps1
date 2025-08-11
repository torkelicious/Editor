param(
    [string]$ProjectPath,
    [switch]$LinuxNative,
    [switch]$FrameworkDependent,
    [switch]$Win,
    [switch]$Linux
)

# Resolve project path
if (-not $ProjectPath)
{
    # Default: go up one level from the script folder and look for Editor.csproj
    $ProjectPath = Join-Path $PSScriptRoot ".." "Editor.csproj"
}
if (-not (Test-Path $ProjectPath))
{
    Write-Error " Project file not found: $ProjectPath"
    exit 1
}

$outputBase = "$HOME/Publish/tgent"

function Clean-Dir($path)
{
    if (Test-Path $path)
    {
        Remove-Item -Path $path -Recurse -Force
    }
}

# If no params build all
if (-not ($LinuxNative -or $FrameworkDependent -or $Win -or $Linux))
{
    $LinuxNative = $true
    $FrameworkDependent = $true
    $Win = $true
    $Linux = $true
}

if ($LinuxNative)
{
    Write-Host "  Publishing Native AOT Linux self-contained...`n"
    Clean-Dir "$outputBase/linuxNative"
    dotnet publish $ProjectPath `
        -c Release `
        -r linux-x64 `
        /p:PublishAot=true `
        /p:PublishTrimmed=true `
        /p:PublishSingleFile=true `
        --self-contained true `
        -o "$outputBase/linuxNative"
}

if ($FrameworkDependent)
{
    Write-Host "  Publishing framework-dependent cross-platform build...`n"
    Clean-Dir "$outputBase/Portable-dependent"
    dotnet publish $ProjectPath `
        -c Release `
        --self-contained false `
        -o "$outputBase/Portable-dependent"
}

if ($Win)
{
    Write-Host "  Publishing self-contained Windows build...`n"
    Clean-Dir "$outputBase/Win"
    dotnet publish $ProjectPath `
        -c Release `
        -r win-x64 `
        /p:PublishTrimmed=true `
        /p:PublishSingleFile=true `
        /p:EnableCompressionInSingleFile=true `
        /p:DebugType=None `
        /p:StripSymbols=true `
        --self-contained true `
        -o "$outputBase/Win"
}

if ($Linux)
{
    Write-Host "  Publishing self-contained Linux build...`n"
    Clean-Dir "$outputBase/Linux"
    dotnet publish $ProjectPath `
        -c Release `
        -r linux-x64 `
        /p:PublishTrimmed=true `
        /p:PublishSingleFile=true `
        /p:EnableCompressionInSingleFile=true `
        /p:DebugType=None `
        /p:StripSymbols=true `
        --self-contained true `
        -o "$outputBase/Linux"
}

Write-Host "`n  All requested builds completed."


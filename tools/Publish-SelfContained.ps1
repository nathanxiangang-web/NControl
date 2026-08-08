[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$AppVersion = "2.0.0"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "src\NControl.App\NControl.App.csproj"
$outputsRoot = Join-Path $projectRoot "outputs"
$normalizedVersion = $AppVersion.TrimStart("v")
if ($normalizedVersion -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
    throw "版本号必须使用语义化格式,例如 2.0.0 或 2.1.0-beta.1: $AppVersion"
}
$folderName = "NControl-v$normalizedVersion-$Runtime-self-contained"
$publishDir = Join-Path $outputsRoot $folderName
$zipPath = Join-Path $outputsRoot "$folderName.zip"
$checksumPath = "$zipPath.sha256"

if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
    throw "未找到项目文件: $projectFile"
}

$requiredPayloads = @(
    (Join-Path $projectRoot "StartAllBack-3.9.4.5256-Stbale-Repack.exe"),
    (Join-Path $projectRoot "GeekUninstaller v1.5.1.162.exe")
)
foreach ($payload in $requiredPayloads) {
    if (-not (Test-Path -LiteralPath $payload -PathType Leaf)) {
        throw "缺少发布载荷: $payload"
    }
}

New-Item -ItemType Directory -Path $outputsRoot -Force | Out-Null

foreach ($target in @($publishDir, $zipPath, $checksumPath)) {
    $resolvedParent = [System.IO.Path]::GetFullPath((Split-Path -Parent $target))
    if ($resolvedParent -ne [System.IO.Path]::GetFullPath($outputsRoot)) {
        throw "拒绝清理 outputs 以外的路径: $target"
    }
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
}

dotnet publish $projectFile `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:DebugSymbols=false `
    -p:DebugType=None `
    -p:Version=$normalizedVersion `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish 失败,退出码: $LASTEXITCODE"
}

$expectedFiles = @(
    (Join-Path $publishDir "NControl.exe"),
    (Join-Path $publishDir "Installers\StartAllBack_setup.exe"),
    (Join-Path $publishDir "Installers\geek.exe"),
    (Join-Path $publishDir "Tools\SecurityCenter\SUPERUSER32.EXE"),
    (Join-Path $publishDir "Tools\SecurityCenter\SUPERUSER64.EXE"),
    (Join-Path $publishDir "Tools\SecurityCenter\KILLSECURITYCENTER.CMD"),
    (Join-Path $publishDir "Tools\SecurityCenter\DEFENDER.CMD")
)
foreach ($expectedFile in $expectedFiles) {
    if (-not (Test-Path -LiteralPath $expectedFile -PathType Leaf)) {
        throw "发布验证失败,缺少文件: $expectedFile"
    }
}

$forbiddenFiles = Get-ChildItem -LiteralPath $publishDir -File -Recurse | Where-Object {
    $_.Extension -in @(".pdb", ".cs", ".sln", ".slnx", ".csproj", ".user", ".log") -or
    $_.FullName -match "[\\/](obj|bin|work|tests?)[\\/]"
}
if ($forbiddenFiles) {
    $names = ($forbiddenFiles.FullName -join [Environment]::NewLine)
    throw "发布目录混入开发文件:`n$names"
}

Compress-Archive -LiteralPath $publishDir -DestinationPath $zipPath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $folderName.zip" | Set-Content -LiteralPath $checksumPath -Encoding ascii

$fileCount = (Get-ChildItem -LiteralPath $publishDir -File -Recurse).Count
$folderBytes = (Get-ChildItem -LiteralPath $publishDir -File -Recurse | Measure-Object -Property Length -Sum).Sum
[pscustomobject]@{
    Version = "v$normalizedVersion"
    Folder = $publishDir
    Zip = $zipPath
    Checksum = $checksumPath
    Files = $fileCount
    FolderMiB = [math]::Round($folderBytes / 1MB, 2)
    ZipMiB = [math]::Round((Get-Item -LiteralPath $zipPath).Length / 1MB, 2)
    SHA256 = $hash
} | Format-List

param([string]$ReleaseDirectory = (Join-Path $PSScriptRoot '便携版'))
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw '未找到 Windows .NET Framework 编译器。' }
$releaseDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ReleaseDirectory)
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $releaseDir '壁纸') -Force | Out-Null
$appIcon = Join-Path $PSScriptRoot 'app.ico'
if (-not (Test-Path -LiteralPath $appIcon)) { throw '缺少图标：app.ico' }
$iconOption = @('/win32icon:app.ico','/resource:app.ico,standup.icon.ico')
$artOptions = @()
foreach ($assetKey in @('tree','lake','garden','forbidden-city','himalaya','mao-reading','zhou-reading','gg-bond','slow-goat','orange-hero-v16')) {
    $assetPath = Join-Path $PSScriptRoot "assets\$assetKey.png"
    if (-not (Test-Path -LiteralPath $assetPath)) { throw "缺少素材：$assetKey" }
    $artOptions += "/resource:$assetPath,art.$assetKey.png"
}
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /win32manifest:app.manifest @iconOption @artOptions "/out:$releaseDir\站起来.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll Program.cs TimerModel.cs QuoteLibrary.cs PixelScene.cs ArtAssets.cs UiTheme.cs AppMark.cs TaskbarIdentity.cs TaskbarEdges.cs ThemeMedallion.cs CompactWindow.cs ReminderPopup.cs SettingsWindow.cs SelfTests.cs AssemblyInfo.cs
if ($LASTEXITCODE -ne 0) { throw '编译失败。' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '使用说明.md') -Destination $releaseDir
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '语录出处.md') -Destination $releaseDir
if (-not (Test-Path -LiteralPath (Join-Path $releaseDir 'quotes.txt'))) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'quotes.txt') -Destination $releaseDir
}
Copy-Item -LiteralPath $appIcon -Destination (Join-Path $releaseDir 'standup-clock-bold.ico')
Write-Output "已生成：$releaseDir\站起来.exe"

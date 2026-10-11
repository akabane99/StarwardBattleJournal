param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $OutputDirectory){$OutputDirectory=Join-Path $repo 'artifacts/windows/starward-battle-journal'}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'}
if(-not(Test-Path -LiteralPath $compiler)){throw '.NET Framework compiler was not found. Build this Windows Forms application on Windows.'}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$sources=Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object {$_.FullName}
$icon=Join-Path $PSScriptRoot 'src/battle.ico'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /r:System.Web.Extensions.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll /r:System.Management.dll ('/win32icon:'+$icon) ('/out:'+(Join-Path $OutputDirectory '星之翼战绩分析.exe')) $sources
if($LASTEXITCODE -ne 0){throw 'Desktop compilation failed.'}
# Copy only distributable resources; never copy a running application's data or browser profile.
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/avatars') -Destination (Join-Path $OutputDirectory 'avatars') -Recurse -Force
foreach($name in @('官网导出指南.html','官网导出助手.js','自动刷新战绩.js')){Copy-Item -LiteralPath (Join-Path $repo ('tools/official/'+$name)) -Destination $OutputDirectory -Force}
Copy-Item -LiteralPath (Join-Path $repo 'docs/WINDOWS-USAGE.txt') -Destination (Join-Path $OutputDirectory '使用说明.txt') -Force
Write-Output ('Windows build: '+$OutputDirectory)

$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'}
$work=Join-Path $repo ('.build/tests-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $work | Out-Null
$sources=Get-ChildItem -LiteralPath (Join-Path $repo 'desktop/src') -Filter '*.cs' | ForEach-Object {$_.FullName}
$exe=Join-Path $work 'SmokeChecks.exe'
& $compiler /nologo /target:exe /main:XzyBattleStats.SmokeChecks /r:System.Web.Extensions.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll /r:System.Management.dll ('/out:'+$exe) $sources (Join-Path $PSScriptRoot 'SmokeChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Smoke test compilation failed.'}
& $exe (Join-Path $repo 'android/assets/demo.json')
if($LASTEXITCODE -ne 0){throw 'Smoke tests failed.'}

param(
 [string]$SdkPath=$env:ANDROID_SDK_ROOT,
 [string]$JdkPath=$env:JAVA_HOME,
 [string]$BuildToolsPath,
 [string]$PlatformPath,
 [string]$Keystore,
 [string]$KeyAlias='battle',
 [string]$StorePasswordFile,
 [string]$KeyPasswordFile
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $SdkPath){$SdkPath=$env:ANDROID_HOME}
if(-not $JdkPath){throw 'Set JAVA_HOME or pass -JdkPath (JDK 17 or newer).'}
if(-not $BuildToolsPath){
 if(-not $SdkPath){throw 'Set ANDROID_SDK_ROOT or pass -SdkPath.'}
 $BuildToolsPath=(Get-ChildItem -LiteralPath (Join-Path $SdkPath 'build-tools') -Directory | Where-Object {$_.Name -match '^\d+\.\d+\.\d+$'} | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1).FullName
}
if(-not $PlatformPath){$PlatformPath=Join-Path $SdkPath 'platforms/android-35'}
$androidJar=Join-Path $PlatformPath 'android.jar'
foreach($required in @($androidJar,(Join-Path $JdkPath 'bin/javac.exe'),(Join-Path $BuildToolsPath 'aapt2.exe'))){if(-not(Test-Path -LiteralPath $required)){throw ('Required build tool not found: '+$required)}}
if($Keystore -and (-not $StorePasswordFile -or -not(Test-Path -LiteralPath $Keystore) -or -not(Test-Path -LiteralPath $StorePasswordFile))){throw 'Signing requires an existing keystore and a password file kept outside the repository.'}
$build=Join-Path $repo ('.build/android-'+[guid]::NewGuid().ToString('N'))
$out=Join-Path $repo 'artifacts/android'
New-Item -ItemType Directory -Force $build,(Join-Path $build 'classes'),(Join-Path $build 'dex'),$out | Out-Null
function Check-Exit{if($LASTEXITCODE -ne 0){throw ('Build tool exited with code '+$LASTEXITCODE)}}
& (Join-Path $BuildToolsPath 'aapt2.exe') compile --dir (Join-Path $PSScriptRoot 'res') -o (Join-Path $build 'res.zip'); Check-Exit
& (Join-Path $BuildToolsPath 'aapt2.exe') link -o (Join-Path $build 'base.apk') -I $androidJar --manifest (Join-Path $PSScriptRoot 'AndroidManifest.xml') -A (Join-Path $PSScriptRoot 'assets') (Join-Path $build 'res.zip'); Check-Exit
$sources=Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.java' -Recurse | ForEach-Object {$_.FullName}
& (Join-Path $JdkPath 'bin/javac.exe') -encoding UTF-8 --release 8 -classpath $androidJar -d (Join-Path $build 'classes') $sources; Check-Exit
& (Join-Path $JdkPath 'bin/jar.exe') cf (Join-Path $build 'classes.jar') -C (Join-Path $build 'classes') .; Check-Exit
& (Join-Path $JdkPath 'bin/java.exe') -cp (Join-Path $BuildToolsPath 'lib/d8.jar') com.android.tools.r8.D8 --min-api 26 --lib $androidJar --output (Join-Path $build 'dex') (Join-Path $build 'classes.jar'); Check-Exit
Copy-Item -LiteralPath (Join-Path $build 'base.apk') -Destination (Join-Path $build 'unsigned.apk')
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open((Join-Path $build 'unsigned.apk'),[IO.Compression.ZipArchiveMode]::Update)
try{[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $build 'dex/classes.dex'),'classes.dex',[IO.Compression.CompressionLevel]::Optimal) | Out-Null}finally{$zip.Dispose()}
$unsigned=Join-Path $out 'starward-battle-journal-unsigned.apk'
& (Join-Path $BuildToolsPath 'zipalign.exe') -f -p 4 (Join-Path $build 'unsigned.apk') $unsigned; Check-Exit
if($Keystore){
 if(-not $KeyPasswordFile){$KeyPasswordFile=$StorePasswordFile}
 $signed=Join-Path $out 'starward-battle-journal.apk'
 & (Join-Path $JdkPath 'bin/java.exe') -jar (Join-Path $BuildToolsPath 'lib/apksigner.jar') sign --ks $Keystore --ks-key-alias $KeyAlias --ks-pass ('file:'+$StorePasswordFile) --key-pass ('file:'+$KeyPasswordFile) --out $signed $unsigned; Check-Exit
 & (Join-Path $JdkPath 'bin/java.exe') -jar (Join-Path $BuildToolsPath 'lib/apksigner.jar') verify --verbose $signed; Check-Exit
 Write-Output ('Signed Android APK: '+$signed)
}else{Write-Output ('Unsigned APK (sign before installing): '+$unsigned)}
& (Join-Path $BuildToolsPath 'zipalign.exe') -c 4 $unsigned; Check-Exit

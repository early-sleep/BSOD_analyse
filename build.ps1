# Build BsodAnalyzer.exe (needs only the .NET Framework compiler shipped with Windows)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'
$outDir = Join-Path $root 'build'
$outExe = Join-Path $outDir 'BsodAnalyzer.exe'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw 'csc.exe not found (.NET Framework 4.x required)' }

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$files = @('Core.cs', 'MainForm.cs', 'Program.cs', 'AssemblyInfo.cs') | ForEach-Object { Join-Path $src $_ }

& $csc /nologo /target:winexe /platform:anycpu /optimize+ /out:$outExe `
    ("/win32manifest:" + (Join-Path $src 'app.manifest')) `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    /r:System.Web.Extensions.dll /r:System.Security.dll `
    $files
if ($LASTEXITCODE -ne 0) { throw "build failed ($LASTEXITCODE)" }
Write-Host ("built: {0} ({1:N0} bytes)" -f $outExe, (Get-Item $outExe).Length)

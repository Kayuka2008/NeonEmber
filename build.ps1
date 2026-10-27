$ErrorActionPreference = 'Stop'
$gameRoot = $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sourcePaths = @('Native.cs','Game.cs','Tactics.cs','World.cs','Program.cs','WorldUi.cs') | ForEach-Object { Join-Path $gameRoot ('src\' + $_) }
$buildRoot = Join-Path $gameRoot 'dev\reports'
New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null
$glyphPath = Join-Path $buildRoot 'glyphs.txt'
$glyphSource = ($sourcePaths | ForEach-Object { Get-Content -Raw -LiteralPath $_ }) -join "`n"
[System.IO.File]::WriteAllText($glyphPath, $glyphSource, [System.Text.Encoding]::UTF8)
& $compilerPath /nologo /target:winexe /platform:x64 /optimize+ "/out:$gameRoot\NeonEmber.exe" "/win32manifest:$gameRoot\src\app.manifest" /r:System.Windows.Forms.dll /r:System.Drawing.dll "/resource:$glyphPath,glyphs.txt" $sourcePaths
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Close the running game before rebuilding.' }
Write-Output "Built: $gameRoot\NeonEmber.exe"


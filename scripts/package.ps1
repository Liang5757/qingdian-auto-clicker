param([string]$Version = '1.0.3')
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'Invalid version.' }
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectVersion = ([xml](Get-Content "$repoRoot/Directory.Build.props" -Raw)).Project.PropertyGroup.Version
if ($Version -ne $projectVersion) { throw "Version $Version does not match project version $projectVersion." }
& "$PSScriptRoot/build.ps1"
$stage = Join-Path $repoRoot "artifacts/staging/$Version-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item "$repoRoot/src/Qingdian.AutoClicker/bin/Release/net48/Qingdian.AutoClicker.exe" $stage
Copy-Item "$repoRoot/src/Qingdian.AutoClicker/bin/Release/net48/Qingdian.AutoClicker.exe.config" $stage
Copy-Item "$repoRoot/README.md", "$repoRoot/README.en.md", "$repoRoot/LICENSE", "$repoRoot/CHANGELOG.md", "$repoRoot/CONTRIBUTING.md", "$repoRoot/CODE_OF_CONDUCT.md", "$repoRoot/SECURITY.md" $stage
Copy-Item "$repoRoot/docs" $stage -Recurse
$archive = Join-Path $repoRoot "artifacts/qingdian-auto-clicker-$Version-windows.zip"
Compress-Archive -Path "$stage/*" -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $archive -Leaf)" | Set-Content -LiteralPath "$archive.sha256" -Encoding ascii
Write-Output "Package: $archive"

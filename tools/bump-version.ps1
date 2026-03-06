param(
    [Parameter(Mandatory=$true)][string]$newver
)

Write-Host "Bumping version to $newver"

# update SdkVersion.cs
(Get-Content SDK/Runtime/Utils/SdkVersion.cs) -replace 'VersionNumber = "[0-9]+\.[0-9]+\.[0-9]+"', "VersionNumber = \"$newver\"" | Set-Content SDK/Runtime/Utils/SdkVersion.cs

# update package.json
$json = Get-Content SDK/package.json -Raw | ConvertFrom-Json
$json.version = $newver
$json | ConvertTo-Json -Depth 5 | Set-Content SDK/package.json

Write-Host "Version updated in SdkVersion.cs and package.json"
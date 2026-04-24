param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path)) {
    throw "Release notes file not found: $Path"
}

$lines = Get-Content -LiteralPath $Path -Encoding UTF8
if ($lines.Count -eq 0) {
    throw "Release notes is empty: $Path"
}

$firstLine = $lines[0].Trim()
if (-not $firstLine.StartsWith('# ')) {
    throw "Invalid release notes format. First line must start with '# '. File: $Path"
}

$title = $firstLine.Substring(2).Trim()
if ([string]::IsNullOrWhiteSpace($title)) {
    throw "Release title is empty in: $Path"
}

$bodyLines = @()
if ($lines.Count -gt 1) {
    $bodyLines = $lines[1..($lines.Count - 1)]
}

$bodyPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("maskedpi-release-body-{0}.md" -f ([guid]::NewGuid().ToString('N')))
$bodyContent = ($bodyLines -join [Environment]::NewLine)
Set-Content -LiteralPath $bodyPath -Value $bodyContent -Encoding UTF8

Write-Host "Parsed release notes:"
Write-Host "- title: $title"
Write-Host "- bodyPath: $bodyPath"

if ($env:GITHUB_OUTPUT) {
    "title=$title" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "body_path=$bodyPath" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}

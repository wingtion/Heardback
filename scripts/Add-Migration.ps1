<#
.SYNOPSIS
  Adds an EF Core migration to a module, e.g. ./scripts/Add-Migration.ps1 -Module Identity -Name AddInviteNotes

.DESCRIPTION
  Generated migration classes are public by default, but modules may only expose their Contracts
  (ADR-0001), so this script makes them internal after generating. Review the migration before committing.
#>
param(
    [Parameter(Mandatory)] [string] $Module,
    [Parameter(Mandatory)] [string] $Name
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src/JobTrail.Modules.$Module"
$migrations = Join-Path $project 'Persistence/Migrations'

dotnet tool restore | Out-Null
dotnet ef migrations add $Name `
    --project $project `
    --startup-project (Join-Path $root 'src/JobTrail.Api') `
    --context "${Module}DbContext" `
    --output-dir Persistence/Migrations
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
Get-ChildItem $migrations -Filter *.cs | ForEach-Object {
    $content = [IO.File]::ReadAllText($_.FullName)
    $content = $content.Replace('public partial class', 'internal partial class').Replace("`r`n", "`n")
    [IO.File]::WriteAllText($_.FullName, $content, $utf8NoBom)
}

Write-Host "Migration '$Name' added to $Module. Review it before committing."

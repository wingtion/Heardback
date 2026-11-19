<#
.SYNOPSIS
  Adds an EF Core migration to a module, e.g. ./scripts/Add-Migration.ps1 -Module Identity -Name AddInviteNotes
  (-Module Llm targets src/Heardback.Infrastructure.Llm, which owns the llm schema.)

.DESCRIPTION
  Generated migration classes are public by default, but modules may only expose their Contracts
  (ADR-0001), so this script makes the new migration and the model snapshot internal after generating.
  Existing migrations are never touched. Review the migration before committing.
#>
param(
    [Parameter(Mandatory)] [string] $Module,
    [Parameter(Mandatory)] [string] $Name
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src/Heardback.Modules.$Module"
if (-not (Test-Path $project)) {
    $project = Join-Path $root "src/Heardback.Infrastructure.$Module"
}
$migrations = Join-Path $project 'Persistence/Migrations'

dotnet tool restore | Out-Null
dotnet ef migrations add $Name `
    --project $project `
    --startup-project (Join-Path $root 'src/Heardback.Api') `
    --context "${Module}DbContext" `
    --output-dir Persistence/Migrations
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Only the files dotnet ef just wrote: the new migration, its designer file and the regenerated snapshot.
$generated = Get-ChildItem $migrations -Filter *.cs | Where-Object {
    $_.Name -like "*_$Name.cs" -or $_.Name -like "*_$Name.Designer.cs" -or $_.Name -like '*ModelSnapshot.cs'
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
foreach ($file in $generated) {
    $content = [IO.File]::ReadAllText($file.FullName)
    $content = $content.Replace('public partial class', 'internal partial class').Replace("`r`n", "`n")
    [IO.File]::WriteAllText($file.FullName, $content, $utf8NoBom)
}

Write-Host "Migration '$Name' added to $Module ($($generated.Count) files made internal). Review it before committing."

# Експорт поточної БД GardenTools у BACPAC для репозиторію.
# Потрібен: dotnet tool install -g microsoft.sqlpackage

param(
    [string]$Server = "localhost\SQLEXPRESS",
    [string]$Database = "GardenToolsDB",
    [string]$Output = "$PSScriptRoot\..\Database\GardenTools.bacpac"
)

$conn = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"
$outDir = Split-Path $Output -Parent
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

Write-Host "Експорт $Database з $Server -> $Output"
sqlpackage /Action:Export /SourceConnectionString:$conn /TargetFile:$Output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Готово."

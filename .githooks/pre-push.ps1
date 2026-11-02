$ErrorActionPreference = 'Stop'
Write-Host "[git-hook: pre-push] Ejecutando suite completa de pruebas de integración de Backend (LocalDB aislado)..."
dotnet test backendSL.Tests/ProjectSaborLatino.Tests.csproj --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    throw "[git-hook: pre-push] Fallaron las pruebas de integración de Backend."
}

$dbs = sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -b -h -1 -W -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name LIKE 'SaborLatinoDb_Test_%';" | Where-Object { $_.Trim() -ne '' }
if ($LASTEXITCODE -ne 0) {
    throw '[git-hook: pre-push] No fue posible comprobar residuos LocalDB.'
}
if ($dbs) {
    throw "[git-hook: pre-push] Quedaron bases temporales residuales en LocalDB: $($dbs -join ', ')"
}

Write-Host "[git-hook: pre-push] Ejecutando verificación completa de Frontend (lint, build y test:ci)..."
node .github/scripts/verify-frontend-ci.mjs --skip-install
if ($LASTEXITCODE -ne 0) {
    throw "[git-hook: pre-push] Falló la verificación de Frontend."
}

Write-Host "[git-hook: pre-push] Todas las puertas de calidad de Backend y Frontend aprobaron."

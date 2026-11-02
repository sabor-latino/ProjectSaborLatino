$ErrorActionPreference = 'Stop'
Write-Host "[git-hook: pre-commit] Verificando formato y limpieza del índice (git diff --cached --check)..."
git diff --cached --check
if ($LASTEXITCODE -ne 0) {
    throw "[git-hook: pre-commit] Falló git diff --cached --check. Corrija espacios en blanco o marcadores de conflicto."
}

Write-Host "[git-hook: pre-commit] Compilando Backend (.NET 10) con -warnaserror..."
dotnet build backendSL/ProjectSaborLatino.csproj --no-restore -warnaserror
if ($LASTEXITCODE -ne 0) {
    throw "[git-hook: pre-commit] Falló la compilación de backendSL."
}

Write-Host "[git-hook: pre-commit] Ejecutando linter, tipos y pruebas de Frontend..."
node .github/scripts/verify-frontend-ci.mjs --skip-install
if ($LASTEXITCODE -ne 0) {
    throw "[git-hook: pre-commit] Falló la verificación de frontendSL."
}

Write-Host "[git-hook: pre-commit] OK."

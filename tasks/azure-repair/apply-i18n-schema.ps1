param([Parameter(Mandatory=$true)][string]$ReviewedSqlHash)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'production-sql-access.ps1')
$sqlPath = Join-Path $PSScriptRoot 'i18n-dynamic.sql'
if ((Get-FileHash -LiteralPath $sqlPath -Algorithm SHA256).Hash -ne $ReviewedSqlHash) { throw 'El SQL cambiÃ³ despuÃ©s de su revisiÃ³n.' }
$sql = [IO.File]::ReadAllText($sqlPath)
if ($sql -notmatch 'SET XACT_ABORT ON' -or $sql -notmatch 'BEGIN CATCH' -or $sql -notmatch 'ROLLBACK TRANSACTION' -or $sql -match '(?m)^GO\s*$') { throw 'El SQL no tiene las garantÃ­as de ejecuciÃ³n revisadas.' }
$result = @{}
Invoke-SaborProductionSql -Action {
    param($connection)
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 30
    $command.CommandText = 'SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;'
    $history = New-Object 'System.Collections.Generic.List[string]'
    $reader = $command.ExecuteReader()
    try { while ($reader.Read()) { $history.Add($reader.GetString(0)) } } finally { $reader.Dispose() }
    $expected = @(
        '20260926160341_InitialCreate',
        '20261006052219_UsuarioPropioYRoles',
        '20261006220938_Catalogo',
        '20261007011559_SolicitudesCotizaciones',
        '20261007040140_Eventos',
        '20261007043719_UsuariosSistema',
        '20261101190334_EvolucionCatalogoPromocionesInicio',
        '20261101205742_GaleriaTituloEn',
        '20261102190453_SolicitudWhatsAppManual'
    )
    if (($history -join ',') -ne ($expected -join ',')) { throw 'La historia de producciÃ³n no coincide con las nueve migraciones revisadas; no se aplicÃ³ SQL.' }

    $result.started_at_utc = [DateTime]::UtcNow.ToString('o')
    $command.CommandTimeout = 120
    $command.CommandText = $sql
    [void]$command.ExecuteNonQuery()

    $command.CommandText = "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261104212320_TraduccionesI18nDinamicas';"
    if ([int]$command.ExecuteScalar() -ne 1) { throw 'No se acreditÃ³ la entrada Ãºnica de la migraciÃ³n aplicada.' }

    $command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name=N'TraduccionesI18n';"
    if ([int]$command.ExecuteScalar() -ne 1) { throw 'La tabla TraduccionesI18n no existe en producciÃ³n.' }

    $command.Dispose()
    $result.completed_at_utc = [DateTime]::UtcNow.ToString('o')
    $result.sql_sha256 = $ReviewedSqlHash
    $result.applied_migration = '20261104212320_TraduccionesI18nDinamicas'
}
$result.temporary_firewall_removed = $true
$json = $result | ConvertTo-Json -Depth 4
$json

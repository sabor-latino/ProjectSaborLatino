param([Parameter(Mandatory=$true)][string]$ReviewedSqlHash)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'production-sql-access.ps1')
$sqlPath = Join-Path $PSScriptRoot 'whatsapp-manual.sql'
if ((Get-FileHash -LiteralPath $sqlPath -Algorithm SHA256).Hash -ne $ReviewedSqlHash) { throw 'El SQL cambió después de su revisión.' }
$sql = [IO.File]::ReadAllText($sqlPath)
if ($sql -notmatch 'SET XACT_ABORT ON' -or $sql -notmatch 'BEGIN CATCH' -or $sql -notmatch 'ROLLBACK TRANSACTION' -or $sql -match '(?m)^GO\s*$') { throw 'El SQL no tiene las garantías de ejecución revisadas.' }
$result = @{}
Invoke-SaborProductionSql -Action {
    param($connection)
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 30
    $command.CommandText = 'SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;'
    $history = New-Object 'System.Collections.Generic.List[string]'
    $reader = $command.ExecuteReader()
    try { while ($reader.Read()) { $history.Add($reader.GetString(0)) } } finally { $reader.Dispose() }
    $expected = @('20260926160341_InitialCreate','20261006052219_UsuarioPropioYRoles','20261006220938_Catalogo','20261007011559_SolicitudesCotizaciones','20261007040140_Eventos','20261007043719_UsuariosSistema','20261101190334_EvolucionCatalogoPromocionesInicio','20261101205742_GaleriaTituloEn')
    if (($history -join ',') -ne ($expected -join ',')) { throw 'La historia de producción no coincide con las ocho migraciones revisadas; no se aplicó SQL.' }
    $command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Solicitudes') AND name IN (N'ClaveIdempotencia',N'HuellaIdempotencia',N'RecibidoWhatsAppEn',N'RecibidoWhatsAppPorUsuarioId');"
    if ([int]$command.ExecuteScalar() -ne 0) { throw 'Esquema parcial o inesperado; no se aplicó SQL.' }
    $command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Solicitudes') AND name=N'CorreoContacto' AND system_type_id=231 AND max_length=512 AND is_nullable=0;"
    if ([int]$command.ExecuteScalar() -ne 1) { throw 'CorreoContacto no coincide con el esquema anterior revisado.' }
    $command.CommandText = 'SELECT COUNT_BIG(*) FROM dbo.Solicitudes;'
    $result.requests_before = [long]$command.ExecuteScalar()
    $result.started_at_utc = [DateTime]::UtcNow.ToString('o')
    $command.CommandTimeout = 120
    $command.CommandText = $sql
    [void]$command.ExecuteNonQuery()
    $command.CommandText = 'SELECT COUNT_BIG(*) FROM dbo.Solicitudes;'
    $result.requests_after = [long]$command.ExecuteScalar()
    $command.CommandText = "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261102190453_SolicitudWhatsAppManual';"
    if ([int]$command.ExecuteScalar() -ne 1) { throw 'No se acreditó la entrada única de la migración aplicada.' }
    $command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Solicitudes') AND name IN (N'ClaveIdempotencia',N'HuellaIdempotencia',N'RecibidoWhatsAppEn',N'RecibidoWhatsAppPorUsuarioId') AND is_nullable=1;"
    if ([int]$command.ExecuteScalar() -ne 4) { throw 'La comprobación posterior de columnas falló; revisar esquema antes de desplegar.' }
    $command.Dispose()
    $result.completed_at_utc = [DateTime]::UtcNow.ToString('o')
    $result.sql_sha256 = $ReviewedSqlHash
    $result.applied_migration = '20261102190453_SolicitudWhatsAppManual'
}
$result.temporary_firewall_removed = $true
$json = $result | ConvertTo-Json -Depth 4
$json | Set-Content -LiteralPath (Join-Path $PSScriptRoot '.artifacts/migration-applied.json') -Encoding UTF8
$json

param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
$result = @{}
. (Join-Path $PSScriptRoot 'production-sql-access.ps1')
Invoke-SaborProductionSql -Action {
    param($connection)
    $queries = [ordered]@{
        target = "SELECT DB_NAME() AS database_name, CONVERT(nvarchar(256),SERVERPROPERTY('ServerName')) AS server_name, CONVERT(int,SERVERPROPERTY('EngineEdition')) AS engine_edition;"
        migrations = 'SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;'
        columns = "SELECT c.name,t.name AS type_name,c.max_length,c.precision,c.scale,c.is_nullable,dc.definition AS default_definition FROM sys.columns c JOIN sys.types t ON c.user_type_id=t.user_type_id LEFT JOIN sys.default_constraints dc ON c.default_object_id=dc.object_id WHERE c.object_id=OBJECT_ID(N'dbo.Solicitudes') AND c.name IN (N'CorreoContacto',N'ClaveIdempotencia',N'HuellaIdempotencia',N'RecibidoWhatsAppEn',N'RecibidoWhatsAppPorUsuarioId') ORDER BY c.name;"
        indexes = "SELECT i.name,i.is_unique,i.is_disabled,i.is_hypothetical,i.filter_definition,c.name AS column_name,ic.key_ordinal,ic.is_included_column FROM sys.indexes i JOIN sys.index_columns ic ON i.object_id=ic.object_id AND i.index_id=ic.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID(N'dbo.Solicitudes') AND i.name=N'IX_Solicitudes_ClaveIdempotencia' ORDER BY ic.key_ordinal;"
        request_counts = 'SELECT COUNT_BIG(*) AS request_count,SUM(CASE WHEN CorreoContacto IS NULL THEN 1 ELSE 0 END) AS null_email_count FROM dbo.Solicitudes;'
    }
    foreach ($entry in $queries.GetEnumerator()) {
        $command = $connection.CreateCommand()
        $command.CommandText = $entry.Value
        $command.CommandTimeout = 30
        $reader = $command.ExecuteReader()
        $table = New-Object System.Data.DataTable
        try { $table.Load($reader) } finally { $reader.Dispose(); $command.Dispose() }
        $result[$entry.Key] = @($table.Rows | ForEach-Object {
            $record = @{}
            foreach ($column in $table.Columns) {
                $value = $_[$column.ColumnName]
                $record[$column.ColumnName] = $(if ($value -is [DBNull]) { $null } else { $value })
            }
            [pscustomobject]$record
        })
        if ($entry.Key -eq 'target') {
            if ($result.target[0].database_name -ne 'SaborLatinoDb' -or $result.target[0].engine_edition -ne 5 -or $result.target[0].server_name -ne 'saborlatino-sql-fr-8dde8f3e') { throw 'Destino SQL inesperado; no continuar.' }
        }
    }
    $result.observed_at_utc = [DateTime]::UtcNow.ToString('o')
}
$result.temporary_firewall_removed = $true
$json = $result | ConvertTo-Json -Depth 6
if ($OutputPath) { $json | Set-Content -LiteralPath $OutputPath -Encoding UTF8 }
$json

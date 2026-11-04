$ErrorActionPreference = 'Stop'
# Solo SELECT de metadatos en Azure SQL. Nunca usa LocalDB ni modifica esquema/datos.
$tokenResult = & az account get-access-token --resource 'https://database.windows.net/' --output json
if ($LASTEXITCODE -ne 0) { throw 'No se obtuvo autorización Azure SQL de solo consulta.' }
$sqlToken = ($tokenResult | ConvertFrom-Json).accessToken
$connection = New-Object System.Data.SqlClient.SqlConnection
$connection.ConnectionString = 'Server=tcp:saborlatino-sql-fr-8dde8f3e.database.windows.net,1433;Initial Catalog=SaborLatinoDb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=20;ApplicationIntent=ReadOnly;Application Name=Codex.AzureDiagnosis.ReadOnly'
$connection.AccessToken = $sqlToken
try {
    $connection.Open()
    $queries = @{
        migrations = 'SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;'
        whatsappColumns = "SELECT name, is_nullable FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Solicitudes') AND name IN (N'CorreoContacto',N'ClaveIdempotencia',N'HuellaIdempotencia',N'RecibidoWhatsAppEn',N'RecibidoWhatsAppPorUsuarioId') ORDER BY name;"
        idempotencyIndex = "SELECT name,is_unique,filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Solicitudes') AND name=N'IX_Solicitudes_ClaveIdempotencia';"
    }
    $result = @{}
    foreach ($entry in $queries.GetEnumerator()) {
        $command = $connection.CreateCommand()
        $command.CommandText = $entry.Value
        $command.CommandTimeout = 20
        $table = New-Object System.Data.DataTable
        $reader = $command.ExecuteReader()
        try { $table.Load($reader) } finally { $reader.Dispose() }
        $result[$entry.Key] = @($table.Rows | ForEach-Object {
            $row = @{}
            foreach ($column in $table.Columns) { $row[$column.ColumnName] = $_[$column.ColumnName] }
            [pscustomobject]$row
        })
        $command.Dispose()
    }
    $result | ConvertTo-Json -Depth 5
} finally {
    $connection.Dispose()
    $sqlToken = $null
    $tokenResult = $null
}

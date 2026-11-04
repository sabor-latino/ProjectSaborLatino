function Invoke-SaborProductionSql {
    param([Parameter(Mandatory=$true)][scriptblock]$Action)
    $ErrorActionPreference = 'Stop'
    $connection = $null
    $createdRules = New-Object 'System.Collections.Generic.List[string]'
    $observedIps = New-Object 'System.Collections.Generic.HashSet[string]'
    $prefix = 'codex-schema-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)
    try {
        $tokenResult = & az account get-access-token --resource 'https://database.windows.net/' -o json
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo obtener token SQL transitorio.' }
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = 'Server=tcp:saborlatino-sql-fr-8dde8f3e.database.windows.net,1433;Initial Catalog=SaborLatinoDb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=20;Pooling=False;Application Name=Codex.AzureRepair'
        $accessToken = ($tokenResult | ConvertFrom-Json).accessToken
        $connection.AccessToken = $accessToken
        $tokenResult = $null
        # Solo IP individuales observadas en errores del servidor SQL, nunca rangos.
        $deadline = [DateTime]::UtcNow.AddMinutes(6)
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try { $connection.Open(); break } catch {
                $message = $_.Exception.ToString()
                if ($attempt -eq 19 -or [DateTime]::UtcNow -gt $deadline -or $message -notmatch "Client with IP address '([0-9.]+)' is not allowed") { throw }
                $sqlIp = [System.Net.IPAddress]::Parse($Matches[1])
                if ($sqlIp.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork -or $sqlIp.ToString() -eq '0.0.0.0') { throw 'SQL no proporcionó una IPv4 individual válida.' }
                $connection.Dispose()
                $connection = New-Object System.Data.SqlClient.SqlConnection
                $connection.ConnectionString = 'Server=tcp:saborlatino-sql-fr-8dde8f3e.database.windows.net,1433;Initial Catalog=SaborLatinoDb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=20;Pooling=False;Application Name=Codex.AzureRepair'
                $connection.AccessToken = $accessToken
                if (-not $observedIps.Add($sqlIp.ToString())) { Start-Sleep -Seconds 15; continue }
                $rule = $prefix + '-' + $attempt
                $createdRules.Add($rule)
                & az sql server firewall-rule create -g rg-saborlatino -s saborlatino-sql-fr-8dde8f3e -n $rule --start-ip-address $sqlIp.ToString() --end-ip-address $sqlIp.ToString() --only-show-errors -o none
                if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear la regla temporal individual.' }
                Start-Sleep -Seconds 5
            }
        }
        $verify = $connection.CreateCommand()
        $verify.CommandTimeout = 30
        $verify.CommandText = "SELECT DB_NAME(),CONVERT(nvarchar(256),SERVERPROPERTY('ServerName')),CONVERT(int,SERVERPROPERTY('EngineEdition'));"
        $reader = $verify.ExecuteReader()
        try {
            if (-not $reader.Read() -or $reader.GetString(0) -ne 'SaborLatinoDb' -or $reader.GetString(1) -ne 'saborlatino-sql-fr-8dde8f3e' -or $reader.GetInt32(2) -ne 5) { throw 'Destino inesperado: se exige Azure SQL del proyecto.' }
        } finally { $reader.Dispose(); $verify.Dispose() }
        & $Action $connection
    } finally {
        if ($connection) { $connection.Dispose() }
        $cleanupErrors = @()
        foreach ($rule in $createdRules) {
            try {
                & az sql server firewall-rule delete -g rg-saborlatino -s saborlatino-sql-fr-8dde8f3e -n $rule --only-show-errors -o none
                if ($LASTEXITCODE -ne 0) { $cleanupErrors += $rule }
            } catch { $cleanupErrors += $rule }
        }
        if ($createdRules.Count -gt 0) {
            $remaining = & az sql server firewall-rule list -g rg-saborlatino -s saborlatino-sql-fr-8dde8f3e --query '[].name' -o json
            if ($LASTEXITCODE -ne 0) { throw 'No se pudo comprobar el cierre del acceso temporal.' }
            $stillPresent = @($remaining | ConvertFrom-Json | Where-Object { $createdRules.Contains($_) })
            foreach ($rule in $stillPresent) {
                try { & az sql server firewall-rule delete -g rg-saborlatino -s saborlatino-sql-fr-8dde8f3e -n $rule --only-show-errors -o none } catch { }
            }
            $remaining = & az sql server firewall-rule list -g rg-saborlatino -s saborlatino-sql-fr-8dde8f3e --query '[].name' -o json
            if ($LASTEXITCODE -ne 0) { throw 'No se pudo acreditar la retirada final de reglas temporales.' }
            $cleanupErrors = @($remaining | ConvertFrom-Json | Where-Object { $createdRules.Contains($_) })
        }
        if ($cleanupErrors.Count -gt 0) { throw ('No se cerraron todas las reglas temporales: ' + ($cleanupErrors -join ', ')) }
        $accessToken = $null
    }
}

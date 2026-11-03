param([switch]$AbrirChrome, [int]$ApiPort = 5238, [int]$PublicPort = 54526, [int]$AdminPort = 54527)
$ErrorActionPreference = 'Stop'
$saborRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $saborRoot

foreach ($port in @($ApiPort, $PublicPort, $AdminPort)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        throw "El puerto $port está ocupado. Compruebe su proceso; este script no detiene servidores ajenos."
    }
}

# Solo base de demostración; los procesos hijos heredan estos valores.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\mssqllocaldb;Database=SaborLatinoLiveDemoDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'
$env:SuperAdmin__Email = 'superadmin@saborlatino.com'
$env:SuperAdmin__Password = 'SaborLatino!2026' # Credencial pública exclusivamente demo, documentada en AGENTS.md.
$env:VITE_API_URL = "http://localhost:$ApiPort"
$env:VITE_PUBLIC_SITE_URL = "http://localhost:$PublicPort"
$env:Cors__OrigenesPermitidos__4 = "http://localhost:$PublicPort"
$env:Cors__OrigenesPermitidos__5 = "http://localhost:$AdminPort"

dotnet ef database update --project backendSL/ProjectSaborLatino.csproj --no-build
if ($LASTEXITCODE -ne 0) { throw 'No se pudo migrar SaborLatinoLiveDemoDb; no se inició ningún servidor.' }

$saborLogRoot = Join-Path $env:TEMP ('sabor-whatsapp-demo-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $saborLogRoot | Out-Null
$api = Start-Process -FilePath 'dotnet' -ArgumentList @('bin/Debug/net10.0/ProjectSaborLatino.dll', '--urls', "http://localhost:$ApiPort") -WorkingDirectory (Join-Path $saborRoot 'backendSL') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $saborLogRoot 'api.out.log') -RedirectStandardError (Join-Path $saborLogRoot 'api.err.log')
$saborFront = Join-Path $saborRoot 'frontendSL'
$publico = Start-Process -FilePath 'node' -ArgumentList @('node_modules/vite/bin/vite.js', '--mode', 'public', '--port', "$PublicPort", '--strictPort') -WorkingDirectory $saborFront -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $saborLogRoot 'public.out.log') -RedirectStandardError (Join-Path $saborLogRoot 'public.err.log')
$admin = Start-Process -FilePath 'node' -ArgumentList @('node_modules/vite/bin/vite.js', '--mode', 'admin', '--port', "$AdminPort", '--strictPort') -WorkingDirectory $saborFront -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $saborLogRoot 'admin.out.log') -RedirectStandardError (Join-Path $saborLogRoot 'admin.err.log')
@{ api = $api.Id; publico = $publico.Id; admin = $admin.Id; logs = $saborLogRoot; workspace = $saborRoot; database = 'SaborLatinoLiveDemoDb' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'demo-processes.local')

if ($AbrirChrome) {
    $saborChrome = Join-Path ${env:ProgramFiles} 'Google/Chrome/Application/chrome.exe'
    if (-not (Test-Path -LiteralPath $saborChrome)) { throw 'Chrome no encontrado; servidores iniciados. Abra las dos URL manualmente.' }
    $saborProfile = Join-Path $saborLogRoot 'chrome-visible'
    Start-Process -FilePath $saborChrome -ArgumentList @("--user-data-dir=`"$saborProfile`"", '--no-first-run', '--new-window', "http://localhost:$PublicPort/solicitar", "http://localhost:$AdminPort/")
}
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'demo-processes.local')

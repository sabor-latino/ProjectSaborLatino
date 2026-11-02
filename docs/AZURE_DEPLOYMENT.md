# Despliegue de Sabor Latino

- Web: https://sabor-latino-seven.vercel.app
- API: https://saborlatino-api-8dde8f3e.azurewebsites.net
- Grupo Azure: rg-saborlatino. Recursos en France Central.
- App Service: saborlatino-api-8dde8f3e, plan plan-saborlatino-free (Windows F1, .NET 10).
- SQL: saborlatino-sql-fr-8dde8f3e, base SaborLatinoDb. Seis migraciones EF aplicadas.
- SQL usa la cuota gratuita y AutoPause al agotarla; acceso mediante identidad administrada de la API.
- CORS permite el dominio de produccion indicado arriba.
- La zona horaria del servidor es Central America Standard Time.
- El frontend se publica desde una copia aislada; no hay integracion automatica de Git configurada.
- Falta configurar las credenciales iniciales del Superadministrador en Azure. No guardar credenciales en Git.

## Incidente de arranque tras auto-pausa: 2026-10-31

Azure SQL puede rechazar la primera conexion con 40613 al reactivarse. La inicializacion del catalogo fallaba antes de arrancar HTTP y IIS respondia 500.30 sin CORS. El navegador mostraba el mensaje generico de API apagada.

DbSeeder reintenta solo la apertura inicial mediante SqlServerRetryingExecutionStrategy: seis reintentos, espera maxima de 15 segundos entre intentos. Se abre IRelationalConnection directamente para evitar que la estrategia predeterminada de EF envuelva la excepcion transitoria e impida reconocerla. Los fallos permanentes siguen propagandose. No se habilitan reintentos globales, para preservar las transacciones explicitas de solicitudes, cotizaciones e inventario.

Regresion: StartupConnectionRetryTests simula un TimeoutException transitorio al abrir una conexion real en una base LocalDB temporal, comprueba inicio HTTP e idempotencia del catalogo, y verifica que un fallo permanente no se reintenta. Evidencia roja previa; dos pruebas nuevas y suite completa de 58 pruebas correctas. No se ha forzado una auto-pausa real en produccion como parte de la prueba.

Los planes gratuitos pueden producir una primera carga lenta. Se mantienen la auto-pausa y la proteccion de cuota; no se ha cambiado a un plan de pago.

## Publicacion y recuperacion

Compilar con dotnet publish backendSL/ProjectSaborLatino.csproj --configuration Release, empaquetar su salida y publicar con az webapp deploy --type zip en la aplicacion indicada. Verificar GET /api/servicios y /api/paquetes con el encabezado Origin del dominio de Vercel.

Durante este despliegue se conservaron dos paquetes locales temporales: saborlatino-api-release.zip (anterior) y saborlatino-api-coldfix.zip (corregido), bajo %TEMP%. La version anterior puede restaurarse publicando su ZIP; conserva el defecto de arranque conocido. Estos archivos temporales no sustituyen un repositorio o un almacen duradero de artefactos.

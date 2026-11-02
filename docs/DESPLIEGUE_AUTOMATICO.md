# Despliegue automático de producción

## Vercel

Los proyectos `sabor-latino` y `sabor-latino-admin` están conectados a `sabor-latino/ProjectSaborLatino`, rama de producción `main`, raíz `frontendSL` y Node.js 22. El público ejecuta `npm run build:public` y publica `dist-public`; el administrador ejecuta `npm run build:admin` y publica `dist-admin`.

- Público: https://sabor-latino-seven.vercel.app
- Administración: https://sabor-latino-admin.vercel.app
- Ambos proyectos tienen `VITE_API_URL` de Azure guardada en Production y Preview. El administrativo tiene `VITE_PUBLIC_SITE_URL` del sitio público.
- La configuración `frontendSL/vercel.json` conserva las rutas de React al entrar directamente a una página.
- Las ramas distintas de `main` generan previews. Los orígenes de preview no están autorizados en CORS de Azure.

## Azure

`.github/workflows/deploy-azure.yml` publica después de un CI exitoso sobre un push a `main`. Valida que el SHA sea el actual de `main` y que CI haya aprobado ese mismo SHA. También admite ejecución manual con los mismos controles.

La identidad usa OIDC de GitHub: no se almacenan secretos de cliente. Su rol Website Contributor se limita al App Service `saborlatino-api-8dde8f3e`, con federación exclusiva de `refs/heads/main`. Los identificadores incluidos en el workflow son públicos y no son credenciales.

Antes de reemplazar la API se compara un hash de los archivos de migraciones y snapshot con el esquema revisado y aplicado en producción. Las ocho migraciones hasta `20261101205742_GaleriaTituloEn` fueron verificadas en Azure el 2026-11-02. Cualquier cambio de esos archivos bloquea la publicación: primero se aplica y revisa la migración en Azure con autorización independiente y después se actualiza el baseline. Esta puerta es una atestación del esquema aplicado, no una consulta SQL en cada ejecución. La identidad de CI no tiene permisos SQL.

Después de publicar se comprueban servicios, paquetes, galería y promociones; además CORS y preflight para los dominios público y administrativo. Los despliegues se serializan. El plan gratuito puede provocar demora de arranque y una breve interrupción durante publicación; no se contrataron recursos de pago.

## Publicación inicial y recuperación

Se publicó el commit fusionado `2d8dab3e485a05077320e396b58702c1a8abb7af` el 2026-11-02. Ambos portales y la API se verificaron por HTTP. La publicación de Vercel se preparó antes de asignar dominios y los artefactos públicos y administrativos se auditaron por separado. Frontend: lint, 10/10 pruebas y ambas compilaciones correctas.

Azure conserva recuperación a un punto en el tiempo durante siete días. Se preservó un ZIP de la API anterior fuera de Git; no se ejecutan migraciones Down para revertir binarios. Las migraciones aplicadas fueron aditivas y la regla temporal de firewall se eliminó. Nunca se conectó ni modificó la base personal LocalDB.

La activación del workflow de Azure requiere fusionar este archivo de workflow en `main`; la primera ejecución verifica OIDC y permisos reales. No se configuraron credenciales nuevas del Superadministrador.

#!/usr/bin/env node
/**
 * Verificador de CI para el frontend (frontendSL).
 *
 * Responsabilidades:
 * 1. Descubrir automáticamente el paquete raíz `frontendSL/package.json` y cualquier
 *    paquete anidado bajo `frontendSL/**` (excluyendo `node_modules`, `dist`, `build`, `.git`).
 * 2. Exigir para CADA paquete detectado (raíz o anidado):
 *    - Un archivo de bloqueo (`package-lock.json` o `npm-shrinkwrap.json`) e instalación con `npm ci`.
 *    - El script `"lint"` en `package.json` y ejecución exitosa de `npm run lint`.
 *    - El script `"build"` en `package.json` y ejecución exitosa de `npm run build`.
 * 3. Hacer cumplir el contrato `test:ci` para pruebas futuras en cada paquete sin enumerar
 *    archivos, componentes, diseños ni carpetas en el workflow:
 *    - Si el paquete define el script `"test:ci"`, se ejecuta siempre con `CI=true`.
 *    - Si existen archivos `*.test.*`, `*.spec.*` o dentro de `__tests__/` y falta `"test:ci"`,
 *      el verificador falla con código de salida 1 y un mensaje claro.
 *    - Si aún no existen pruebas ni `"test:ci"`, deja constancia explícita de que solo se
 *      verificaron `lint` y `build` (sin afirmar cobertura de interacción ni apariencia).
 */

import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const IGNORED_DIRS = new Set(['node_modules', 'dist', 'build', '.git', 'coverage', '.vite']);
const TEST_FILE_REGEX = /\.(test|spec)\.[cm]?[jt]sx?$/i;
const CODE_FILE_REGEX = /\.[cm]?[jt]sx?$/i;

function parseArgs(argv) {
  let rootDir = path.resolve(process.cwd(), 'frontendSL');
  let skipInstall = false;

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--skip-install') {
      skipInstall = true;
    } else if (arg === '--root' && i + 1 < argv.length) {
      rootDir = path.resolve(process.cwd(), argv[++i]);
    }
  }

  return { rootDir, skipInstall };
}

function discoverPackageDirs(dir, result = []) {
  const pkgJsonPath = path.join(dir, 'package.json');
  if (fs.existsSync(pkgJsonPath)) {
    result.push(dir);
  }

  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    if (!entry.isDirectory() || IGNORED_DIRS.has(entry.name)) {
      continue;
    }
    discoverPackageDirs(path.join(dir, entry.name), result);
  }

  return result;
}

function findTestFiles(packageDir, allPackageDirs) {
  const nestedPackageDirs = new Set(
    allPackageDirs.filter((d) => d !== packageDir && d.startsWith(packageDir + path.sep))
  );
  const found = [];

  function walk(currentDir, insideTestsFolder) {
    const entries = fs.readdirSync(currentDir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(currentDir, entry.name);
      if (entry.isDirectory()) {
        if (IGNORED_DIRS.has(entry.name) || nestedPackageDirs.has(fullPath)) {
          continue;
        }
        walk(fullPath, insideTestsFolder || entry.name === '__tests__');
      } else if (entry.isFile()) {
        if (TEST_FILE_REGEX.test(entry.name) || (insideTestsFolder && CODE_FILE_REGEX.test(entry.name))) {
          found.push(path.relative(packageDir, fullPath));
        }
      }
    }
  }

  walk(packageDir, false);
  return found.sort();
}

function runCommand(command, args, cwd, extraEnv = {}) {
  const display = `${command} ${args.join(' ')}`;
  console.log(`\n[frontend-ci] (${path.relative(process.cwd(), cwd) || '.'}) $ ${display}`);

  const result = spawnSync(command, args, {
    cwd,
    stdio: 'inherit',
    shell: process.platform === 'win32',
    env: {
      ...process.env,
      CI: 'true',
      ...extraEnv
    }
  });

  if (result.error) {
    console.error(`[frontend-ci] Error al ejecutar "${display}": ${result.error.message}`);
    process.exit(1);
  }

  if (result.status !== 0) {
    console.error(`[frontend-ci] El comando "${display}" falló con código de salida ${result.status}.`);
    process.exit(result.status ?? 1);
  }
}

function main() {
  const { rootDir, skipInstall } = parseArgs(process.argv.slice(2));

  if (!fs.existsSync(rootDir)) {
    console.error(`[frontend-ci] No existe el directorio raíz del frontend: ${rootDir}`);
    process.exit(1);
  }

  const rootPkgJson = path.join(rootDir, 'package.json');
  if (!fs.existsSync(rootPkgJson)) {
    console.error(`[frontend-ci] No se encontró package.json en la raíz del frontend: ${rootPkgJson}`);
    process.exit(1);
  }

  const packageDirs = discoverPackageDirs(rootDir);
  console.log(`[frontend-ci] Paquetes detectados bajo ${rootDir}: ${packageDirs.length}`);

  for (const pkgDir of packageDirs) {
    const relPkgDir = path.relative(process.cwd(), pkgDir) || '.';
    const pkgJson = JSON.parse(fs.readFileSync(path.join(pkgDir, 'package.json'), 'utf8'));
    const scripts = pkgJson.scripts ?? {};

    console.log(`\n============================================================`);
    console.log(`[frontend-ci] Verificando paquete: ${pkgJson.name ?? relPkgDir} (${relPkgDir})`);
    console.log(`============================================================`);

    const hasLockfile =
      fs.existsSync(path.join(pkgDir, 'package-lock.json')) ||
      fs.existsSync(path.join(pkgDir, 'npm-shrinkwrap.json'));

    if (!hasLockfile) {
      console.error(
        `[frontend-ci] ERROR DE CONTRATO CI: El paquete "${relPkgDir}" no contiene package-lock.json ni npm-shrinkwrap.json. ` +
          `Todo paquete de frontend (raíz o anidado) exige un lockfile para instalar dependencias de forma reproducible con "npm ci".`
      );
      process.exit(1);
    }

    if (typeof scripts.lint !== 'string' || scripts.lint.trim().length === 0) {
      console.error(
        `[frontend-ci] ERROR DE CONTRATO CI: El paquete "${relPkgDir}" debe definir obligatoriamente el script "lint" en package.json.`
      );
      process.exit(1);
    }

    if (typeof scripts.build !== 'string' || scripts.build.trim().length === 0) {
      console.error(
        `[frontend-ci] ERROR DE CONTRATO CI: El paquete "${relPkgDir}" debe definir obligatoriamente el script "build" en package.json.`
      );
      process.exit(1);
    }

    if (!skipInstall) {
      runCommand('npm', ['ci'], pkgDir);
    }

    runCommand('npm', ['run', 'lint'], pkgDir);
    runCommand('npm', ['run', 'build'], pkgDir);

    const detectedTestFiles = findTestFiles(pkgDir, packageDirs);
    const hasTestCiScript = typeof scripts['test:ci'] === 'string' && scripts['test:ci'].trim().length > 0;

    if (hasTestCiScript) {
      console.log(
        `[frontend-ci] Script "test:ci" detectado en ${relPkgDir} (${detectedTestFiles.length} archivo(s) de prueba encontrados). Ejecutando suite no interactiva...`
      );
      runCommand('npm', ['run', 'test:ci'], pkgDir);
    } else if (detectedTestFiles.length > 0) {
      console.error(
        `\n[frontend-ci] ERROR DE CONTRATO CI: Se detectaron ${detectedTestFiles.length} archivo(s) de prueba en "${relPkgDir}", ` +
          `pero package.json no define el script "test:ci".\n` +
          `Archivos detectados:\n  - ${detectedTestFiles.slice(0, 20).join('\n  - ')}\n` +
          `Defina "test:ci" en ${path.join(relPkgDir, 'package.json')} para ejecutar el runner de pruebas de forma no interactiva en CI.`
      );
      process.exit(1);
    } else {
      console.log(
        `\n[frontend-ci] AVISO: "${relPkgDir}" superó "npm run lint" y "npm run build" (análisis estático ESLint, compilación TypeScript y empaquetado Vite).\n` +
          `[frontend-ci] No existen todavía archivos de prueba (*.test.*, *.spec.*) ni script "test:ci" en "${relPkgDir}".\n` +
          `[frontend-ci] Nota: lint y build no verifican interacciones de usuario en navegador ni apariencia visual.`
      );
    }
  }

  console.log('\n[frontend-ci] Verificación de frontend finalizada correctamente.');
}

main();

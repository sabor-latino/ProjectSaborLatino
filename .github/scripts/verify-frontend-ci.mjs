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
 * 3. Hacer cumplir el contrato `test:ci` obligatorio para cada paquete:
 *    - Debe existir el script `"test:ci"` y al menos un archivo de prueba (`*.test.*`, `*.spec.*` o dentro de `__tests__/`).
 *    - Se ejecuta siempre `npm run test:ci` con `CI=true`.
 */

import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const IGNORED_DIRS = new Set(['node_modules', 'dist-public', 'dist-admin', 'dist', 'build', '.git', 'coverage', '.vite']);
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
    process.exit(1);
  }
}

function verifyPortalBuilds(packageDir) {
  const publicPages = /^src\/paginas\/(Inicio|Galeria|Paquetes|PreguntasFrecuentes|SolicitarCotizacion)\.tsx$/;
  for (const [directory, portal, ownEntry, forbidden] of [
    ['dist-public', 'publico', 'src/AppPublico.tsx', /^(src\/AppAdmin\.tsx|src\/paginas\/Login\.tsx|src\/paginas\/panel\/)/],
    ['dist-admin', 'admin', 'src/AppAdmin.tsx', /^src\/AppPublico\.tsx$/],
  ]) {
    const outputDir = path.join(packageDir, directory);
    const reportPath = path.join(outputDir, '.vite', 'portal-modules.json');
    if (!fs.existsSync(reportPath)) throw new Error('[frontend-ci] Falta informe de módulos: ' + reportPath);
    const report = JSON.parse(fs.readFileSync(reportPath, 'utf8'));
    if (report.portal !== portal || !Array.isArray(report.modules) ||
        !report.modules.every(module => typeof module === 'string' && module.startsWith('src/')) ||
        !report.modules.includes(ownEntry)) {
      throw new Error('[frontend-ci] Informe inválido o portal incompleto: ' + directory);
    }
    const leaked = report.modules.filter(module => forbidden.test(module) || (portal === 'admin' && publicPages.test(module)));
    if (leaked.length) throw new Error('[frontend-ci] ' + directory + ' incluye módulos del otro portal: ' + leaked.join(', '));
    const scripts = [];
    function readScripts(dir) {
      for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) readScripts(fullPath);
        else if (entry.isFile() && /\.m?js$/.test(entry.name)) scripts.push(fs.readFileSync(fullPath, 'utf8'));
      }
    }
    readScripts(outputDir);
    if (!scripts.length || (portal === 'publico' && scripts.some(script => script.includes('Portal Administrativo')))) {
      throw new Error('[frontend-ci] ' + directory + ' vacío o contiene texto exclusivo administrativo.');
    }
    console.log('[frontend-ci] ' + directory + ': ' + report.modules.length + ' módulos fuente; separación comprobada.');
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
    if (pkgDir === rootDir) {
      for (const script of ['build:public', 'build:admin']) {
        if (typeof scripts[script] !== 'string' || !scripts[script].trim()) throw new Error('[frontend-ci] Falta script raíz ' + script);
        runCommand('npm', ['run', script], pkgDir);
      }
      verifyPortalBuilds(pkgDir);
    }

    const detectedTestFiles = findTestFiles(pkgDir, packageDirs);
    const hasTestCiScript = typeof scripts['test:ci'] === 'string' && scripts['test:ci'].trim().length > 0;

    if (hasTestCiScript && detectedTestFiles.length > 0) {
      console.log(
        `[frontend-ci] Script "test:ci" detectado en ${relPkgDir} (${detectedTestFiles.length} archivo(s) de prueba encontrados). Ejecutando suite no interactiva...`
      );
      runCommand('npm', ['run', 'test:ci'], pkgDir);
    } else if (!hasTestCiScript) {
      console.error(
        `\n[frontend-ci] ERROR DE CONTRATO CI: El paquete "${relPkgDir}" debe definir obligatoriamente el script "test:ci" en package.json para ejecutar las pruebas automatizadas.`
      );
      process.exit(1);
    } else {
      console.error(
        `\n[frontend-ci] ERROR DE CONTRATO CI: El paquete "${relPkgDir}" define "test:ci" pero no contiene archivos de prueba (*.test.*, *.spec.* o __tests__/).`
      );
      process.exit(1);
    }
  }

  console.log('\n[frontend-ci] Verificación de frontend finalizada correctamente.');
}

main();

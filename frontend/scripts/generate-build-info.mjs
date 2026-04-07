import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = dirname(fileURLToPath(import.meta.url));
const frontendRoot = resolve(scriptDir, '..');
const outputPath = resolve(frontendRoot, 'src', 'app', 'shared', 'build-info.ts');
const packageJsonPath = resolve(frontendRoot, 'package.json');
const releaseManifestPath = resolve(frontendRoot, '..', 'release-please-manifest.json');

const packageJson = JSON.parse(readFileSync(packageJsonPath, 'utf8'));

const appVersion = resolveVersion();
const buildDate = resolveBuildDate();

mkdirSync(dirname(outputPath), { recursive: true });
writeFileSync(
  outputPath,
  [
    'export const buildInfo = {',
    `  version: ${JSON.stringify(appVersion)},`,
    `  buildDate: ${JSON.stringify(buildDate)}`,
    '} as const;',
    ''
  ].join('\n'),
  'utf8'
);

function resolveVersion() {
  const explicitVersion = process.env.APP_VERSION?.trim();
  if (explicitVersion) {
    return explicitVersion;
  }

  if (existsSync(releaseManifestPath)) {
    const releaseManifest = JSON.parse(readFileSync(releaseManifestPath, 'utf8'));
    const releaseVersion = releaseManifest['.'];
    if (typeof releaseVersion === 'string' && releaseVersion.trim().length > 0) {
      return `v${releaseVersion.trim()}`;
    }
  }

  const packageVersion = packageJson.version?.trim();
  if (packageVersion) {
    return packageVersion;
  }

  return 'dev';
}

function resolveBuildDate() {
  const explicitBuildDate = process.env.BUILD_DATE?.trim();
  if (explicitBuildDate) {
    return explicitBuildDate;
  }

  return new Date().toISOString();
}

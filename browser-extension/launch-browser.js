import { chromium } from '@playwright/test';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import { existsSync, mkdirSync } from 'fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const pathToExtension = join(__dirname, 'dist');
const manifestPath = join(pathToExtension, 'manifest.json');
const userDataDir = join(__dirname, '.playwright-user-data');

if (!existsSync(manifestPath)) {
  console.error(`Error: Extension manifest not found at ${manifestPath}`);
  console.error('Please run the build command first: npm run build');
  process.exit(1);
}

if (!existsSync(userDataDir)) {
  mkdirSync(userDataDir);
}

(async () => {
  console.log(`Launching browser with extension from: ${pathToExtension}`);
  
  const browserContext = await chromium.launchPersistentContext(userDataDir, {
    headless: false,
    args: [
      `--disable-extensions-except=${pathToExtension}`,
      `--load-extension=${pathToExtension}`,
    ],
  });

  console.log('Browser launched. Close the browser to stop the script.');
  
  // Wait for the browser to be closed manually
  await new Promise(resolve => {
    browserContext.on('close', resolve);
  });

  process.exit(0);
})();

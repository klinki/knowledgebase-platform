import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const distDir = path.join(__dirname, '..', 'dist');

if (fs.existsSync(distDir)) {
  console.log(`Cleaning ${distDir}...`);
  fs.rmSync(distDir, { recursive: true, force: true });
  console.log('Cleaned successfully.');
} else {
  console.log('Nothing to clean.');
}

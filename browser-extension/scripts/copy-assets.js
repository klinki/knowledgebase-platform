import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.join(__dirname, '..');
const distDir = path.join(rootDir, 'dist');

const assets = [
  { src: 'manifest.json', dest: 'manifest.json' },
  { src: 'popup.html', dest: 'popup.html' },
  { src: 'options.html', dest: 'options.html' },
  { src: 'src/content.css', dest: 'content.css' }
];

const directories = [
  { src: 'icons', dest: 'icons' }
];

function ensureDir(dir) {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

function copyFile(src, dest) {
  const fullSrc = path.join(rootDir, src);
  const fullDest = path.join(distDir, dest);
  
  if (fs.existsSync(fullSrc)) {
    console.log(`Copying ${src} to ${dest}...`);
    ensureDir(path.dirname(fullDest));
    fs.copyFileSync(fullSrc, fullDest);
  } else {
    console.warn(`Warning: Source file ${fullSrc} not found.`);
  }
}

function copyDir(src, dest) {
  const fullSrc = path.join(rootDir, src);
  const fullDest = path.join(distDir, dest);

  if (fs.existsSync(fullSrc)) {
    console.log(`Copying directory ${src} to ${dest}...`);
    ensureDir(fullDest);
    const files = fs.readdirSync(fullSrc);
    for (const file of files) {
      const srcFile = path.join(fullSrc, file);
      const destFile = path.join(fullDest, file);
      if (fs.statSync(srcFile).isDirectory()) {
        copyDir(path.join(src, file), path.join(dest, file));
      } else {
        fs.copyFileSync(srcFile, destFile);
      }
    }
  } else {
    console.warn(`Warning: Source directory ${fullSrc} not found.`);
  }
}

// Ensure dist exists
ensureDir(distDir);

// Copy individual files
assets.forEach(asset => copyFile(asset.src, asset.dest));

// Copy directories
directories.forEach(dir => copyDir(dir.src, dir.dest));

console.log('Assets copied successfully.');

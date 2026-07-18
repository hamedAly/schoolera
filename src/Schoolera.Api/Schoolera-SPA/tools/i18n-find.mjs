/**
 * Project-owned missing-key scanner for Angular 22 + Transloco.
 *
 * Why not `@jsverse/transloco-keys-manager find` directly?
 * - Package peer requires `@angular/compiler` `>=21.1.0 <22.0.0` while this SPA is Angular 22.
 * - On Angular 22 the detective hangs inside template extraction (CPU spin / never exits).
 * - Cosmiconfig also required BOM-free `angular.json` and a CJS config (`transloco.config.cjs`).
 *
 * This scanner enforces the same release gate: static keys used in app sources must exist
 * in both `ar` and `en` files. Extra catalog keys are informational only.
 */
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, extname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const i18nRoot = join(root, 'public', 'i18n');
const scanRoots = [
  join(root, 'src', 'app', 'features'),
  join(root, 'src', 'app', 'shared'),
  join(root, 'src', 'app', 'core', 'auth'),
  join(root, 'src', 'app', 'core', 'http'),
  join(root, 'src', 'app', 'core', 'i18n'),
  join(root, 'src', 'app', 'core', 'layout'),
  join(root, 'src', 'app', 'core', 'seo'),
  join(root, 'src', 'app', 'core', 'config'),
  join(root, 'src', 'app', 'testing'),
];

const SOURCE_EXT = new Set(['.ts', '.html']);
const SKIP_DIR = new Set(['api-client', 'node_modules', 'dist', '.angular']);

const KEY_PATTERNS = [
  /['`]([a-zA-Z][\w.-]*\.[a-zA-Z][\w.-]*)['`]\s*\|\s*transloco/g,
  /(?:translate|selectTranslate)\(\s*['`]([a-zA-Z][\w.-]*\.[a-zA-Z][\w.-]*)['`]/g,
  /\.translate\(\s*['`]([a-zA-Z][\w.-]*\.[a-zA-Z][\w.-]*)['`]/g,
];

function flatten(value, prefix = '', out = new Set()) {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) {
    if (prefix) out.add(prefix);
    return out;
  }
  for (const [key, child] of Object.entries(value)) {
    const next = prefix ? `${prefix}.${key}` : key;
    if (child !== null && typeof child === 'object' && !Array.isArray(child)) {
      flatten(child, next, out);
    } else {
      out.add(next);
    }
  }
  return out;
}

function readJson(path) {
  return JSON.parse(readFileSync(path, 'utf8'));
}

function collectLangPairs(dir) {
  const pairs = [];
  const entries = readdirSync(dir);
  if (entries.includes('ar.json') && entries.includes('en.json')) {
    pairs.push({
      scope: relative(i18nRoot, dir).replace(/\\/g, '/') || '',
      ar: join(dir, 'ar.json'),
      en: join(dir, 'en.json'),
    });
  }
  for (const entry of entries) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      pairs.push(...collectLangPairs(full));
    }
  }
  return pairs;
}

function collectTranslationKeys() {
  const keys = new Set();
  for (const pair of collectLangPairs(i18nRoot)) {
    const prefix = pair.scope ? `${pair.scope}.` : '';
    for (const key of flatten(readJson(pair.ar))) {
      keys.add(`${prefix}${key}`);
    }
    for (const key of flatten(readJson(pair.en))) {
      keys.add(`${prefix}${key}`);
    }
  }
  return keys;
}

function collectSourceFiles(dir, out = []) {
  for (const entry of readdirSync(dir)) {
    if (SKIP_DIR.has(entry)) continue;
    const full = join(dir, entry);
    const st = statSync(full);
    if (st.isDirectory()) {
      collectSourceFiles(full, out);
      continue;
    }
    if (SOURCE_EXT.has(extname(entry))) out.push(full);
  }
  return out;
}

function extractKeysFromSource(filePath) {
  const text = readFileSync(filePath, 'utf8');
  const found = new Set();
  for (const pattern of KEY_PATTERNS) {
    pattern.lastIndex = 0;
    let match;
    while ((match = pattern.exec(text)) !== null) {
      const key = match[1];
      if (!key.includes('${')) found.add(key);
    }
  }
  return found;
}

console.log('Starting search for missing Transloco keys (Angular 22-compatible scanner)...');

const catalog = collectTranslationKeys();
const used = new Set();
const files = scanRoots.flatMap((dir) => collectSourceFiles(dir));

for (const file of files) {
  for (const key of extractKeysFromSource(file)) used.add(key);
}

const missing = [...used].filter((key) => !catalog.has(key)).sort();

if (missing.length === 0) {
  console.log(
    `\nNo missing keys. Scanned ${files.length} files; ${used.size} static keys referenced; catalog ${catalog.size}.`,
  );
  process.exit(0);
}

console.error(`\nMissing translation keys (${missing.length}):`);
for (const key of missing) console.error(`  - ${key}`);
process.exit(1);

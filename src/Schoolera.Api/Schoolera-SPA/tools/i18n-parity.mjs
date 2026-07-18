/**
 * Deterministic Arabic/English translation tree parity checker.
 * Fails with exit code 1 when required key sets differ or JSON is invalid.
 */
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const i18nRoot = join(root, 'public', 'i18n');

function flatten(value, prefix = '', out = new Set()) {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) {
    if (prefix) {
      out.add(prefix);
    }
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
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch (error) {
    throw new Error(`Invalid JSON: ${path}\n${error instanceof Error ? error.message : error}`);
  }
}

function collectLangPairs(dir) {
  const pairs = [];
  const entries = readdirSync(dir);

  const hasAr = entries.includes('ar.json');
  const hasEn = entries.includes('en.json');
  if (hasAr || hasEn) {
    if (!hasAr || !hasEn) {
      throw new Error(`Missing ar/en pair in ${relative(root, dir)}`);
    }
    pairs.push({
      scope: relative(i18nRoot, dir) || 'root',
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

function diff(a, b) {
  return [...a].filter((key) => !b.has(key)).sort();
}

let failed = false;
const pairs = collectLangPairs(i18nRoot);

for (const pair of pairs) {
  const arKeys = flatten(readJson(pair.ar));
  const enKeys = flatten(readJson(pair.en));
  const missingInEn = diff(arKeys, enKeys);
  const missingInAr = diff(enKeys, arKeys);

  if (missingInEn.length === 0 && missingInAr.length === 0) {
    console.log(`[ok] ${pair.scope}: ${arKeys.size} keys`);
    continue;
  }

  failed = true;
  console.error(`[fail] ${pair.scope}`);
  if (missingInEn.length) {
    console.error(`  Missing in en.json (${missingInEn.length}): ${missingInEn.join(', ')}`);
  }
  if (missingInAr.length) {
    console.error(`  Missing in ar.json (${missingInAr.length}): ${missingInAr.join(', ')}`);
  }
}

if (failed) {
  process.exit(1);
}

console.log('Translation parity check passed.');

import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dirname, "..", "..");
const installPath = path.join(repoRoot, "scriptable", "KTV_Pro_V8_3_Install_Cache.js");
const source = fs.readFileSync(installPath, "utf8");

function testInstallClearsStaleGitHubLibraryCache() {
  assert.match(source, /RESET_GITHUB_LIBRARY_CACHE_ON_INSTALL\s*=\s*true/);
  assert.match(source, /"master\.csv"/);
  assert.match(source, /"github_local_songs\.csv"/);
  assert.match(source, /function\s+resetStaleLibraryCache\s*\(/);
}

function testInstallKeepsLocalSongsCsv() {
  const staleFilesMatch = source.match(/const STALE_LIBRARY_FILES = \[[\s\S]*?\];/);
  assert.ok(staleFilesMatch, "STALE_LIBRARY_FILES should exist");
  const entries = [...staleFilesMatch[0].matchAll(/"([^"]+)"/g)].map(match => match[1]);
  assert.ok(entries.includes("github_local_songs.csv"));
  assert.ok(!entries.includes("local_songs.csv"));
}

testInstallClearsStaleGitHubLibraryCache();
testInstallKeepsLocalSongsCsv();
console.log("install-cache-reset tests passed");

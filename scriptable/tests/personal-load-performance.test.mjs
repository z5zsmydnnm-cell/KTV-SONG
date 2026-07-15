import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dirname, "..", "..");
const personalPath = path.join(repoRoot, "scriptable", "KTV_Pro_V8_3_Personal.js");
const source = fs.readFileSync(personalPath, "utf8");

const loadSongsMatch = source.match(/async function loadSongs\(\) \{[\s\S]*?\n\}/);
assert.ok(loadSongsMatch, "loadSongs function should exist");

function testLoadSongsUsesLinearDeduplication() {
  const loadSongsSource = loadSongsMatch[0];
  assert.doesNotMatch(loadSongsSource, /findIndex\s*\(/);
  assert.match(loadSongsSource, /new Set\(\)/);
}

testLoadSongsUsesLinearDeduplication();
console.log("personal-load-performance tests passed");

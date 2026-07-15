import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dirname, "..", "..");
const personalPath = path.join(repoRoot, "scriptable", "KTV_Pro_V8_3_Personal.js");
const source = fs.readFileSync(personalPath, "utf8");

const updateLibraryMatch = source.match(/async function updateLibrary\(\) \{[\s\S]*?\n\}/);
assert.ok(updateLibraryMatch, "updateLibrary function should exist");

function testUpdateLibraryAllowsMissingOrEmptyMasterCsv() {
  const updateLibrarySource = updateLibraryMatch[0];
  assert.match(source, /const EMPTY_MASTER_CSV =/);
  assert.match(updateLibrarySource, /fetchTextNoCache\(CSV_URL,\s*true\)/);
  assert.match(updateLibrarySource, /if \(txt === null\) txt = EMPTY_MASTER_CSV;/);
  assert.doesNotMatch(updateLibrarySource, /rows\.length === 0\) throw/);
}

testUpdateLibraryAllowsMissingOrEmptyMasterCsv();
console.log("update-library-empty-master tests passed");

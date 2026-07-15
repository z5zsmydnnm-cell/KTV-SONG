import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dirname, "..", "..");
const personalPath = path.join(repoRoot, "scriptable", "KTV_Pro_V8_3_Personal.js");
const source = fs.readFileSync(personalPath, "utf8");

function testDoesNotTouchICloudAtModuleStartup() {
  assert.doesNotMatch(source, /const\s+iCloudFM\s*=\s*FileManager\.iCloud\(\)/);
  assert.match(source, /function\s+getICloudFM\s*\(/);
}

function testDoesNotMigrateICloudDataAtModuleStartup() {
  assert.doesNotMatch(source, /\nmigrateLegacyData\(\);\s*\n/);
}

testDoesNotTouchICloudAtModuleStartup();
testDoesNotMigrateICloudDataAtModuleStartup();
console.log("personal-startup tests passed");

import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import vm from "node:vm";

const repoRoot = path.resolve(import.meta.dirname, "..", "..");
const loaderPath = path.join(repoRoot, "scriptable", "KTV_Pro_V8_3_Loader.js");
const loaderSource = fs.readFileSync(loaderPath, "utf8");
const cachedScriptPath = "/docs/KTV_PRO_V8/KTV_Pro_V8_3_Personal.cached.js";
const remoteScript = "// KTV Pro V8.3 Personal\n" +
  "globalThis.__ktvRunCount = (globalThis.__ktvRunCount || 0) + 1;";

class FakeFileManager {
  constructor(files) {
    this.files = files;
    this.createdDirectories = [];
  }

  documentsDirectory() {
    return "/docs";
  }

  joinPath(left, right) {
    return String(left).replace(/\/$/, "") + "/" + right;
  }

  fileExists(filePath) {
    return Object.prototype.hasOwnProperty.call(this.files, filePath);
  }

  createDirectory(filePath) {
    this.createdDirectories.push(filePath);
  }

  readString(filePath) {
    if (!this.fileExists(filePath)) throw new Error("Missing file: " + filePath);
    return this.files[filePath];
  }

  writeString(filePath, text) {
    this.files[filePath] = text;
  }
}

async function runLoader(initialFiles = {}) {
  const files = { ...initialFiles };
  let requestCount = 0;
  let requestedUrl = "";
  const fm = new FakeFileManager(files);

  class FakeRequest {
    constructor(url) {
      requestCount++;
      requestedUrl = url;
      this.url = url;
      this.headers = {};
      this.response = { statusCode: 200 };
    }

    async loadString() {
      return remoteScript;
    }
  }

  class FakeAlert {
    constructor() {
      this.title = "";
      this.message = "";
    }

    addAction() {}
    async presentAlert() {}
  }

  const context = {
    Alert: FakeAlert,
    Request: FakeRequest,
    FileManager: { local: () => fm },
    console,
    globalThis: {}
  };
  context.globalThis = context;
  vm.createContext(context);
  await vm.runInContext("(async function(){\n" + loaderSource + "\n})()", context);

  return { files, requestCount, requestedUrl, runCount: context.__ktvRunCount || 0 };
}

async function testUsesCachedScriptWithoutNetwork() {
  const cachedScript = "// KTV Pro V8.3 Personal\n" +
    "globalThis.__ktvRunCount = (globalThis.__ktvRunCount || 0) + 1;";

  const result = await runLoader({ [cachedScriptPath]: cachedScript });

  assert.equal(result.requestCount, 0);
  assert.equal(result.runCount, 1);
  assert.equal(result.files[cachedScriptPath], cachedScript);
}

async function testDownloadsAndCachesWhenNoCachedScriptExists() {
  const result = await runLoader();

  assert.equal(result.requestCount, 1);
  assert.match(result.requestedUrl, /KTV_Pro_V8_3_Personal\.js/);
  assert.equal(result.files[cachedScriptPath], remoteScript);
  assert.equal(result.runCount, 1);
}

await testUsesCachedScriptWithoutNetwork();
await testDownloadsAndCachesWhenNoCachedScriptExists();
console.log("loader-cache tests passed");

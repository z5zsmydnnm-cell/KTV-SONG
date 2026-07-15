// KTV Pro V8.3 Personal - offline Scriptable loader
// Daily-use loader. It never connects to GitHub.
// Run KTV_Pro_V8_3_Install_Cache.js only when you want to install or update the cache.

const CACHE_DIR_NAME = "KTV_PRO_V8";
const CACHE_FILE_NAME = "KTV_Pro_V8_3_Personal.cached.js";
const SCRIPT_MARKER = "KTV Pro V8.3 Personal";

const fm = FileManager.local();
const cacheDir = fm.joinPath(fm.documentsDirectory(), CACHE_DIR_NAME);
const cachePath = fm.joinPath(cacheDir, CACHE_FILE_NAME);

try {
  ensureCacheDir();

  const code = readCachedScript();
  if (!isValidScript(code)) {
    throw new Error(
      "Install cache first.\n\n" +
      "Run this Scriptable script once:\n" +
      "KTV_Pro_V8_3_Install_Cache.js\n\n" +
      "After install, open this loader again."
    );
  }

  await runScript(code);
} catch (e) {
  writeLocalStatus("error", String(e));
  const a = new Alert();
  a.title = "KTV Loader";
  a.message = String(e);
  a.addAction("OK");
  await a.presentAlert();
}

function ensureCacheDir() {
  if (!fm.fileExists(cacheDir)) {
    fm.createDirectory(cacheDir);
  }
}

function readCachedScript() {
  try {
    if (!fm.fileExists(cachePath)) return "";
    return fm.readString(cachePath);
  } catch (e) {
    return "";
  }
}

function isValidScript(code) {
  return !!code && String(code).indexOf(SCRIPT_MARKER) >= 0;
}

async function runScript(code) {
  writeLocalStatus("local-cache", "Using Scriptable local cached script.");
  await eval("(async function(){\n" + code + "\n})()");
}

function writeLocalStatus(source, message) {
  const text =
    "source=" + source + "\n" +
    "time=" + new Date().toISOString() + "\n" +
    "localCache=Scriptable local documents/" + CACHE_DIR_NAME + "/" + CACHE_FILE_NAME + "\n" +
    "message=" + message + "\n";
  try {
    fm.writeString(fm.joinPath(cacheDir, "loader-status.txt"), text);
  } catch (e) {}
}

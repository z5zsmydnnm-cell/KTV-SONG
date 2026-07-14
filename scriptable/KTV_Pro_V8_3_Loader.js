// KTV Pro V8.3 Personal - cached Scriptable loader
// Paste this small file into Scriptable. It runs the cached full script first,
// and only downloads from GitHub when the cache is missing or broken.

const SCRIPT_URL = "https://raw.githubusercontent.com/z5zsmydnnm-cell/KTV-SONG/agent/build-001-manager-v6/scriptable/KTV_Pro_V8_3_Personal.js";
const CACHE_DIR_NAME = "KTV_PRO_V8";
const CACHE_FILE_NAME = "KTV_Pro_V8_3_Personal.cached.js";
const SCRIPT_MARKER = "KTV Pro V8.3 Personal";
const STATUS_FILE_NAME = "loader-status.txt";

const fm = FileManager.local();
const visibleFM = FileManager.iCloud();
const cacheDir = fm.joinPath(fm.documentsDirectory(), CACHE_DIR_NAME);
const cachePath = fm.joinPath(cacheDir, CACHE_FILE_NAME);
const visibleDir = visibleFM.joinPath(visibleFM.documentsDirectory(), CACHE_DIR_NAME);
const visibleStatusPath = visibleFM.joinPath(visibleDir, STATUS_FILE_NAME);

try {
  ensureCacheDir();

  let code = readCachedScript();
  let usedCache = isValidScript(code);

  if (!usedCache) {
    code = await downloadAndCacheScript();
  } else {
    writeStatus("local-cache", "Using iPhone local cached script.");
  }

  await runScript(code);
} catch (e) {
  writeStatus("error", String(e));
  const a = new Alert();
  a.title = "KTV Loader Error";
  a.message = String(e);
  a.addAction("OK");
  await a.presentAlert();
}

function ensureCacheDir() {
  if (!fm.fileExists(cacheDir)) {
    fm.createDirectory(cacheDir);
  }
  try {
    if (!visibleFM.fileExists(visibleDir)) {
      visibleFM.createDirectory(visibleDir);
    }
  } catch (e) {}
}

function readCachedScript() {
  try {
    if (!fm.fileExists(cachePath)) return "";
    return fm.readString(cachePath);
  } catch (e) {
    return "";
  }
}

async function downloadAndCacheScript() {
  const req = new Request(SCRIPT_URL + "?t=" + Date.now());
  req.headers = { "Cache-Control": "no-cache", "Pragma": "no-cache" };
  const code = await req.loadString();
  const status = req.response ? req.response.statusCode : 200;
  if (status >= 400) throw new Error("HTTP " + status + "\n" + SCRIPT_URL);
  if (!isValidScript(code)) throw new Error("Downloaded script content is not valid.");
  fm.writeString(cachePath, code);
  writeVisibleFile(CACHE_FILE_NAME, code);
  writeStatus("github-download", "Downloaded script from GitHub and cached it on iPhone.");
  return code;
}

function isValidScript(code) {
  return !!code && String(code).indexOf(SCRIPT_MARKER) >= 0;
}

async function runScript(code) {
  await eval("(async function(){\n" + code + "\n})()");
}

function writeStatus(source, message) {
  const text =
    "source=" + source + "\n" +
    "time=" + new Date().toISOString() + "\n" +
    "localCache=Scriptable local documents/" + CACHE_DIR_NAME + "/" + CACHE_FILE_NAME + "\n" +
    "visibleFolder=iCloud Drive/Scriptable/" + CACHE_DIR_NAME + "\n" +
    "message=" + message + "\n";
  try {
    fm.writeString(fm.joinPath(cacheDir, STATUS_FILE_NAME), text);
  } catch (e) {}
  try {
    if (!visibleFM.fileExists(visibleDir)) {
      visibleFM.createDirectory(visibleDir);
    }
    visibleFM.writeString(visibleStatusPath, text);
  } catch (e) {}
}

function writeVisibleFile(name, text) {
  try {
    if (!visibleFM.fileExists(visibleDir)) {
      visibleFM.createDirectory(visibleDir);
    }
    visibleFM.writeString(visibleFM.joinPath(visibleDir, name), text);
  } catch (e) {}
}

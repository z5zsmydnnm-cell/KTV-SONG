// KTV Pro V8.3 Personal - tiny Scriptable loader
// Paste only this file into Scriptable. It downloads the full script from GitHub.

const SCRIPT_URL = "https://raw.githubusercontent.com/z5zsmydnnm-cell/KTV-SONG/refs/heads/agent/build-001-manager-v6/scriptable/KTV_Pro_V8_3_Personal.js";

try {
  const req = new Request(SCRIPT_URL + "?t=" + Date.now());
  req.headers = { "Cache-Control": "no-cache", "Pragma": "no-cache" };
  const code = await req.loadString();
  const status = req.response ? req.response.statusCode : 200;
  if (status >= 400) throw new Error("HTTP " + status + "\n" + SCRIPT_URL);
  if (!code || code.indexOf("KTV Pro V8.3 Personal") < 0) throw new Error("Downloaded script content is not valid.");
  await eval("(async function(){\n" + code + "\n})()");
} catch (e) {
  const a = new Alert();
  a.title = "KTV Loader Error";
  a.message = String(e);
  a.addAction("OK");
  await a.presentAlert();
}

// KTV Pro V8.3 Personal - Scriptable
// 修正版：支援 GitHub main 下載、raw 快取避開、舊 CSV 格式、WPF 新 CSV 格式、手動新增/修改/刪除、最愛、點唱佇列、YouTube。

const APP = "KTV Pro V8.3 Personal";
const OWNER = "z5zsmydnnm-cell";
const REPO = "KTV-SONG";
const BRANCH = "agent/build-001-manager-v6";
const BASE = "https://raw.githubusercontent.com/" + OWNER + "/" + REPO + "/" + BRANCH;
const CSV_URL = BASE + "/songs/master.csv";
const IPHONE_CSV_REPO_PATH = "songs/iphone-local-songs.csv";
const IPHONE_CSV_URL = BASE + "/" + IPHONE_CSV_REPO_PATH;
const VERSION_URL = BASE + "/version.json";

const iCloudFM = FileManager.iCloud();
const fm = FileManager.local();
const localFM = fm;
const dir = fm.documentsDirectory();
const dataDir = fm.joinPath(dir, "KTV_PRO_V8");
if (!fm.fileExists(dataDir)) fm.createDirectory(dataDir);
const legacyDataDir = iCloudFM.joinPath(iCloudFM.documentsDirectory(), "KTV_PRO_V8");

const csvPath = fm.joinPath(dataDir, "master.csv");
const localCsvPath = fm.joinPath(dataDir, "local_songs.csv");
const githubLocalCsvPath = fm.joinPath(dataDir, "github_local_songs.csv");
const favPath = fm.joinPath(dataDir, "favorites.json");
const queuePath = fm.joinPath(dataDir, "queue.json");
const recentPath = fm.joinPath(dataDir, "recent.json");
const statsPath = fm.joinPath(dataDir, "stats.json");
const settingPath = fm.joinPath(dataDir, "settings.json");
const deletedPath = fm.joinPath(dataDir, "deleted.json");

migrateLegacyData();

let songs = [];
let localSongs = [];
let favorites = loadJson(favPath, []);
let queue = loadJson(queuePath, []);
let recent = loadJson(recentPath, []);
let stats = loadJson(statsPath, {});
let settings = loadJson(settingPath, { autoFavorite: true });
let deletedKeys = loadJson(deletedPath, []);


async function mainMenu() {
  while (true) {
    let youtubeSongs = songs.filter(s => s.youtube && String(s.youtube).trim() !== "");
    let a = new Alert();
    a.title = APP;
    a.message =
      "歌庫：" + songs.length + " 首\n" +
      "YouTube 連結：" + youtubeSongs.length + " 首\n" +
      "點唱佇列：" + queue.length + " 首\n" +
      "我的最愛：" + favorites.length + " 首";

    a.addAction("更新 GitHub 歌庫");
    a.addAction("同步本機到 GitHub");
    a.addAction("從 iPhone 檔案匯入 CSV");
    a.addAction("匯入本機 CSV 歌單");
    a.addAction("手動新增歌曲");
    a.addAction("搜尋歌曲");
    a.addAction("YouTube 最愛");
    a.addAction("我的最愛");
    a.addAction("點唱佇列");
    a.addAction("最近點唱");
    a.addAction("統計 TOP20");
    a.addAction("Apple TV 顯示");
    a.addAction("設定");
    a.addAction("版本資訊");
    a.addCancelAction("結束");

    let r = await a.presentSheet();
    if (r < 0) return;

    if (r === 0) await updateLibrary();
    if (r === 1) await syncLocalToGitHub();
    if (r === 2) await importCSVFromFiles();
    if (r === 3) await importLocalCSV();
    if (r === 4) await manualAddSong();
    if (r === 5) await searchFlow();
    if (r === 6) await youtubeFavoriteMenu();
    if (r === 7) await listSongs(favorites, "我的最愛");
    if (r === 8) await queueMenu();
    if (r === 9) await listSongs(recent, "最近點唱");
    if (r === 10) await showStats();
    if (r === 11) await showAppleTV();
    if (r === 12) await settingMenu();
    if (r === 13) await versionInfo();
  }
}

async function importCSVFromFiles() {
  try {
    let paths = await DocumentPicker.open(["public.comma-separated-values-text", "public.text", "public.data"]);
    if (!paths || paths.length === 0) return;

    let p = paths[0];
    let txt = "";
    try {
      txt = localFM.readString(p);
    } catch (e) {
      txt = fm.readString(p);
    }

    let rows = csvToSongs(txt).filter(s => s.title || s.singer || s.yinyuan || s.jinsang || s.hongyin || s.youtube);
    if (rows.length === 0) {
      await msg("匯入失敗", "CSV 沒有讀到歌曲資料");
      return;
    }

    localSongs = rows.concat(localSongs);
    saveLocalSongs();
    await loadSongs();
    await msg("匯入完成", "已從 iPhone 檔案匯入 " + rows.length + " 首\n目前歌庫：" + songs.length + " 首");
  } catch (e) {
    await msg("匯入取消或失敗", String(e));
  }
}

async function manualAddSong() {
  let s = await inputSong({});
  if (!s) return;
  localSongs.unshift(s);
  try {
    saveLocalSongs();
  } catch (e) {
    await msg("新增失敗", "無法寫入本機歌單：\n" + String(e));
    return;
  }

  await loadSongs();
  await msg("新增完成", s.title + "\n已儲存到 iPhone 本機 KTV_PRO_V8/local_songs.csv\n本機新增：" + localSongs.length + " 首");
}

async function editSong(s) {
  let oldKey = songKey(s);
  let edited = await inputSong(s);
  if (!edited) return;

  deletedKeys = Array.from(new Set(deletedKeys.concat([oldKey])));
  saveJson(deletedPath, deletedKeys);

  localSongs = localSongs.filter(x => songKey(x) !== oldKey);
  localSongs.unshift(edited);
  saveLocalSongs();

  replaceInList(favorites, oldKey, edited, favPath);
  replaceInList(queue, oldKey, edited, queuePath);
  replaceInList(recent, oldKey, edited, recentPath);

  await loadSongs();
  await msg("修改完成", edited.title);
}

async function deleteSong(s) {
  let a = new Alert();
  a.title = "確認刪除";
  a.message = (s.title || "") + "\n" + (s.singer || "");
  a.addDestructiveAction("刪除");
  a.addCancelAction("取消");
  let r = await a.presentSheet();
  if (r < 0) return;

  let k = songKey(s);
  deletedKeys = Array.from(new Set(deletedKeys.concat([k])));
  saveJson(deletedPath, deletedKeys);

  localSongs = localSongs.filter(x => songKey(x) !== k);
  favorites = favorites.filter(x => songKey(x) !== k);
  queue = queue.filter(x => songKey(x) !== k);
  recent = recent.filter(x => songKey(x) !== k);

  saveLocalSongs();
  saveJson(favPath, favorites);
  saveJson(queuePath, queue);
  saveJson(recentPath, recent);

  await loadSongs();
  await msg("已刪除", s.title || "");
}

async function inputSong(old) {
  let title = await prompt("歌曲資料", "歌名", old.title || "");
  if (title === null || title.trim() === "") return null;

  let singer = await prompt("歌曲資料", "歌手，可空白", old.singer || "");
  if (singer === null) singer = "";

  let lang = await prompt("歌曲資料", "語言，可空白", old.lang || "");
  if (lang === null) lang = "";

  let yinyuan = await prompt("歌曲資料", "音圓歌號，可空白", old.yinyuan || "");
  if (yinyuan === null) yinyuan = "";

  let jinsang = await prompt("歌曲資料", "金嗓歌號，可空白", old.jinsang || "");
  if (jinsang === null) jinsang = "";

  let hongyin = await prompt("歌曲資料", "弘音歌號，可空白", old.hongyin || "");
  if (hongyin === null) hongyin = "";

  let youtube = await prompt("歌曲資料", "YouTube 連結，可空白", old.youtube || "");
  if (youtube === null) youtube = "";

  let note = await prompt("歌曲資料", "備註，可空白", old.note || "");
  if (note === null) note = "";

  return {
    title: title.trim(),
    singer: singer.trim(),
    lang: lang.trim(),
    yinyuan: yinyuan.trim(),
    jinsang: jinsang.trim(),
    hongyin: hongyin.trim(),
    youtube: youtube.trim(),
    note: note.trim()
  };
}

async function loadSongs() {
  songs = [];
  localSongs = [];

  if (fm.fileExists(csvPath)) {
    try {
      songs = songs.concat(csvToSongs(fm.readString(csvPath)));
    } catch (e) {}
  }

  if (fm.fileExists(githubLocalCsvPath)) {
    try {
      songs = songs.concat(csvToSongs(fm.readString(githubLocalCsvPath)));
    } catch (e) {}
  }

  if (fm.fileExists(localCsvPath)) {
    try {
      localSongs = csvToSongs(fm.readString(localCsvPath));
      songs = songs.concat(localSongs);
    } catch (e) {}
  }

  songs = songs
    .filter(s => s.title || s.singer || s.yinyuan || s.jinsang || s.hongyin || s.youtube)
    .filter(s => !deletedKeys.includes(songKey(s)))
    .filter((s, i, arr) => arr.findIndex(x => songKey(x) === songKey(s)) === i);
}

function saveLocalSongs() {
  let header = "歌名,歌手,語言,音圓,金嗓,弘音,YouTube,備註\n";
  let body = localSongs.map(s => [
    s.title, s.singer, s.lang, s.yinyuan, s.jinsang, s.hongyin, s.youtube, s.note
  ].map(csvCell).join(",")).join("\n");
  fm.writeString(localCsvPath, header + body + (body ? "\n" : ""));
  let saved = csvToSongs(fm.readString(localCsvPath));
  if (saved.length !== localSongs.length) {
    throw new Error("local_songs.csv 寫入驗證失敗，預期 " + localSongs.length + " 首，實際 " + saved.length + " 首。");
  }
}

async function updateLibrary() {
  try {
    let txt = await fetchTextNoCache(CSV_URL, false);
    let rows = csvToSongs(txt);
    if (rows.length === 0) throw new Error("GitHub CSV 已下載，但解析後是 0 首。請確認 songs/master.csv 欄位。");

    fm.writeString(csvPath, txt);

    let iphoneRows = [];
    let iphoneTxt = await fetchTextNoCache(IPHONE_CSV_URL, true);
    if (iphoneTxt !== null) {
      iphoneRows = csvToSongs(iphoneTxt);
      fm.writeString(githubLocalCsvPath, iphoneTxt);
    }

    await loadSongs();
    await msg("更新完成", "GitHub 讀取成功\n主歌庫：" + rows.length + " 首\niPhone 同步：" + iphoneRows.length + " 首\n目前共：" + songs.length + " 首\n分支：" + BRANCH);
  } catch (e) {
    await msg("更新失敗", String(e) + "\n\nURL:\n" + CSV_URL);
  }
}

async function syncLocalToGitHub() {
  try {
    if (fm.fileExists(localCsvPath)) {
      localSongs = csvToSongs(fm.readString(localCsvPath));
    }

    if (!localSongs || localSongs.length === 0) {
      await msg("沒有可同步資料", "目前沒有 iPhone 手動新增/修改的本機歌曲。");
      return;
    }

    let token = await getGitHubToken();
    if (!token) return;

    let csv = songsToScriptableCsv(dedupeSongs(localSongs));
    let masterTxt = await fetchTextNoCache(CSV_URL, true);
    let masterCsv = songsToMasterCsv(csvToSongs(masterTxt || "").concat(localSongs));

    await putGitHubTextFile(
      IPHONE_CSV_REPO_PATH,
      csv,
      "data: sync iphone local songs " + new Date().toISOString(),
      token);
    await putGitHubTextFile(
      "songs/master.csv",
      masterCsv,
      "data: sync master csv from iphone " + new Date().toISOString(),
      token);

    fm.writeString(githubLocalCsvPath, csv);
    fm.writeString(csvPath, masterCsv);
    await loadSongs();
    await msg("同步完成", "已同步 " + localSongs.length + " 首到 GitHub：\n" + IPHONE_CSV_REPO_PATH + "\n/songs/master.csv");
  } catch (e) {
    await msg("同步失敗", String(e) + "\n\n請確認 GitHub Token 有 Contents Read/Write 權限。");
  }
}

async function importLocalCSV() {
  if (!fm.fileExists(localCsvPath)) {
    fm.writeString(localCsvPath, "歌名,歌手,語言,音圓,金嗓,弘音,YouTube,備註\n");
    await msg("已建立本機歌單檔", "KTV_PRO_V8/local_songs.csv");
    return;
  }
  await loadSongs();
  await msg("匯入完成", "目前共 " + songs.length + " 首");
}

async function searchFlow() {
  while (true) {
    let q = await prompt("搜尋歌曲", "輸入歌名、歌手、歌號或備註", "");
    if (q === null || q.trim() === "") return;

    let key = q.trim().toLowerCase();
    let found = songs.filter(s => [
      s.title, s.singer, s.lang, s.yinyuan, s.jinsang, s.hongyin, s.youtube, s.note
    ].join(" ").toLowerCase().includes(key)).slice(0, 80);

    if (found.length === 0) {
      await msg("沒有找到", q);
      continue;
    }
    await listSongs(found, "搜尋結果 " + found.length + " 首");
  }
}

async function youtubeFavoriteMenu() {
  let list = songs.filter(s => s.youtube && String(s.youtube).trim() !== "");
  if (list.length === 0) {
    await msg("YouTube 最愛", "目前沒有 YouTube 連結歌曲");
    return;
  }

  while (true) {
    let a = new Alert();
    a.title = "YouTube 最愛";
    a.message = "共 " + list.length + " 首";

    let showList = list.slice(0, 40);
    for (let s of showList) a.addAction((s.title || "未命名") + " / " + (s.singer || ""));

    a.addCancelAction("返回");
    let r = await a.presentSheet();
    if (r < 0) return;

    await openYoutubeOrSearch(showList[r]);
  }
}

async function openYoutubeOrSearch(s) {
  if (s.youtube && String(s.youtube).trim() !== "") {
    Safari.open(String(s.youtube).trim());
  } else {
    Safari.open("https://www.youtube.com/results?search_query=" +
      encodeURIComponent((s.title || "") + " " + (s.singer || "") + " KTV"));
  }
}

async function listSongs(list, title) {
  if (!list || list.length === 0) {
    await msg(title, "沒有資料");
    return;
  }

  while (true) {
    let a = new Alert();
    a.title = title;

    let showList = list.slice(0, 30);
    for (let s of showList) {
      a.addAction((s.title || "未命名") + " / " + (s.singer || "") + " / " + firstCode(s));
    }

    a.addCancelAction("返回");
    let i = await a.presentSheet();
    if (i < 0) return;

    await songDetail(showList[i]);
  }
}

async function songDetail(s) {
  while (true) {
    let a = new Alert();
    a.title = s.title || "歌曲";
    a.message =
      "歌手：" + (s.singer || "") +
      "\n語言：" + (s.lang || "") +
      "\n音圓：" + (s.yinyuan || "") +
      "\n金嗓：" + (s.jinsang || "") +
      "\n弘音：" + (s.hongyin || "") +
      "\nYouTube：" + (s.youtube || "") +
      "\n備註：" + (s.note || "");

    a.addAction("加入點唱");
    a.addAction(isFav(s) ? "取消最愛" : "加入最愛");
    a.addAction("直接開 YouTube");
    a.addAction("修改歌曲資料");
    a.addDestructiveAction("刪除歌曲");
    a.addAction("複製歌號");
    a.addCancelAction("返回");

    let r = await a.presentSheet();
    if (r < 0) return;

    if (r === 0) {
      queue.unshift(s);
      queue = dedupeSongs(queue);
      saveJson(queuePath, queue);
      recordPlay(s);
      recent.unshift(s);
      recent = dedupeSongs(recent).slice(0, 100);
      saveJson(recentPath, recent);
      await msg("已加入點唱", s.title || "");
    }

    if (r === 1) {
      toggleFavorite(s);
      await msg(isFav(s) ? "已加入最愛" : "已取消最愛", s.title || "");
    }

    if (r === 2) await openYoutubeOrSearch(s);
    if (r === 3) await editSong(s);
    if (r === 4) {
      await deleteSong(s);
      return;
    }
    if (r === 5) {
      Pasteboard.copyString(firstCode(s));
      await msg("已複製", firstCode(s));
    }
  }
}

async function queueMenu() {
  while (true) {
    let a = new Alert();
    a.title = "點唱佇列";
    a.message = "共 " + queue.length + " 首";
    a.addAction("查看佇列");
    a.addAction("清空佇列");
    a.addCancelAction("返回");
    let r = await a.presentSheet();
    if (r < 0) return;
    if (r === 0) await listSongs(queue, "點唱佇列");
    if (r === 1) {
      queue = [];
      saveJson(queuePath, queue);
      await msg("已清空", "點唱佇列");
    }
  }
}

async function showStats() {
  let entries = Object.keys(stats)
    .map(k => ({ key: k, count: stats[k] }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 20);

  if (entries.length === 0) {
    await msg("統計 TOP20", "目前沒有點唱紀錄");
    return;
  }

  let text = entries.map((x, i) => (i + 1) + ". " + x.key + " - " + x.count + " 次").join("\n");
  await msg("統計 TOP20", text);
}

async function showAppleTV() {
  let html = "<html><head><meta name='viewport' content='width=device-width, initial-scale=1'>" +
    "<style>body{font-family:-apple-system;background:#101820;color:white;padding:28px}h1{font-size:42px}li{font-size:30px;margin:16px 0}</style></head><body>" +
    "<h1>KTV 點唱佇列</h1><ol>" +
    queue.slice(0, 20).map(s => "<li>" + escapeHtml(s.title || "") + " / " + escapeHtml(s.singer || "") + "</li>").join("") +
    "</ol></body></html>";
  let w = new WebView();
  await w.loadHTML(html);
  await w.present(true);
}

async function settingMenu() {
  let a = new Alert();
  a.title = "設定";
  a.message =
    "GitHub CSV:\n" + CSV_URL +
    "\n\niPhone 同步檔:\n" + IPHONE_CSV_REPO_PATH +
    "\n\n本機資料夾:\nKTV_PRO_V8" +
    "\n\n目前歌庫：" + songs.length + " 首" +
    "\nGitHub Token：" + (settings.githubToken ? "已設定" : "未設定");
  a.addAction("刪除 GitHub 快取 master.csv");
  a.addAction("重設刪除紀錄");
  a.addAction("設定/更換 GitHub Token");
  a.addDestructiveAction("清除 GitHub Token");
  a.addCancelAction("返回");
  let r = await a.presentSheet();
  if (r < 0) return;

  if (r === 0) {
    if (fm.fileExists(csvPath)) fm.remove(csvPath);
    if (fm.fileExists(githubLocalCsvPath)) fm.remove(githubLocalCsvPath);
    await loadSongs();
    await msg("已刪除快取", "請重新更新 GitHub 歌庫");
  }
  if (r === 1) {
    deletedKeys = [];
    saveJson(deletedPath, deletedKeys);
    await loadSongs();
    await msg("已重設", "刪除紀錄已清空");
  }
  if (r === 2) {
    settings.githubToken = "";
    await getGitHubToken();
  }
  if (r === 3) {
    settings.githubToken = "";
    saveJson(settingPath, settings);
    await msg("已清除", "GitHub Token 已清除");
  }
}

async function versionInfo() {
  let info = "";
  try {
    let req = new Request(VERSION_URL + "?t=" + Date.now());
    info = await req.loadString();
  } catch (e) {
    info = "無法讀取 version.json\n" + String(e);
  }
  await msg("版本資訊", APP + "\n分支：" + BRANCH + "\n\n" + info);
}

async function fetchTextNoCache(url, allowNotFound) {
  let req = new Request(url + "?t=" + Date.now());
  req.headers = {
    "Cache-Control": "no-cache",
    "Pragma": "no-cache"
  };
  let txt = await req.loadString();
  let status = req.response ? req.response.statusCode : 200;
  if (status === 404 && allowNotFound) return null;
  if (status >= 400) throw new Error("HTTP " + status + " " + url);
  return txt;
}

async function getGitHubToken() {
  if (settings.githubToken && String(settings.githubToken).trim() !== "") {
    return String(settings.githubToken).trim();
  }

  let token = await prompt(
    "GitHub Token",
    "請貼上 GitHub fine-grained token。需要 Repository Contents: Read and write 權限。",
    ""
  );
  if (token === null || token.trim() === "") return "";

  settings.githubToken = token.trim();
  saveJson(settingPath, settings);
  return settings.githubToken;
}

async function githubJson(method, url, token, body) {
  let req = new Request(url);
  req.method = method;
  req.headers = {
    "Accept": "application/vnd.github+json",
    "Authorization": "Bearer " + token,
    "X-GitHub-Api-Version": "2022-11-28",
    "User-Agent": "KTV-Pro-Scriptable"
  };
  if (body !== null && body !== undefined) {
    req.headers["Content-Type"] = "application/json";
    req.body = JSON.stringify(body);
  }

  let txt = await req.loadString();
  let status = req.response ? req.response.statusCode : 200;
  let data = {};
  try {
    data = txt ? JSON.parse(txt) : {};
  } catch (e) {
    data = { raw: txt };
  }

  if (status >= 400) {
    let message = data && data.message ? data.message : txt;
    throw new Error("GitHub HTTP " + status + ": " + message);
  }
  return data;
}

async function putGitHubTextFile(path, text, message, token) {
  let apiUrl = "https://api.github.com/repos/" + OWNER + "/" + REPO + "/contents/" + path;
  let sha = "";

  try {
    let existing = await githubJson("GET", apiUrl + "?ref=" + encodeURIComponent(BRANCH), token, null);
    sha = existing && existing.sha ? existing.sha : "";
  } catch (e) {
    // 404 means the file does not exist yet. Other errors will be caught by PUT if permissions are wrong.
  }

  let body = {
    message: message,
    content: Data.fromString(text).toBase64String(),
    branch: BRANCH
  };
  if (sha) body.sha = sha;

  return await githubJson("PUT", apiUrl, token, body);
}

function csvToSongs(text) {
  let rows = parseCSV(stripBom(text || ""));
  if (rows.length === 0) return [];

  let headers = rows[0].map(h => normalizeHeader(h));
  let out = [];
  for (let i = 1; i < rows.length; i++) {
    let row = rows[i];
    if (!row || row.every(c => String(c || "").trim() === "")) continue;
    out.push(rowToSongByHeader(headers, row));
  }
  return out;
}

function songsToScriptableCsv(list) {
  let header = "歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,YouTube,備註\n";
  let body = list.map(s => [
    s.title,
    s.singer,
    s.lang,
    s.yinyuan,
    s.jinsang,
    s.hongyin,
    s.youtube,
    s.note
  ].map(csvCell).join(",")).join("\n");
  return header + body + (body ? "\n" : "");
}

function songsToMasterCsv(list) {
  let merged = mergeSongsForMaster(list);
  let header = "歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,集數,備註\n";
  let body = merged.map(s => [
    s.title,
    s.singer,
    s.lang,
    s.yinyuan,
    s.jinsang,
    s.hongyin,
    s.volume || "",
    s.note || ""
  ].map(csvCell).join(",")).join("\n");
  return "\uFEFF" + header + body + (body ? "\n" : "");
}

function mergeSongsForMaster(list) {
  let bySong = {};
  for (let s of list) {
    if (!s || (!s.title && !s.singer && !firstCode(s))) continue;
    let key = [clean(s.title).toLowerCase(), clean(s.singer).toLowerCase(), clean(s.lang).toLowerCase()].join("|");
    if (!bySong[key]) {
      bySong[key] = {
        title: clean(s.title),
        singer: clean(s.singer),
        lang: clean(s.lang),
        yinyuan: "",
        jinsang: "",
        hongyin: "",
        volume: "",
        note: clean(s.note)
      };
    }

    bySong[key].yinyuan = chooseCode(bySong[key].yinyuan, s.yinyuan);
    bySong[key].jinsang = chooseCode(bySong[key].jinsang, s.jinsang);
    bySong[key].hongyin = chooseCode(bySong[key].hongyin, s.hongyin);
    bySong[key].volume = mergeListText(bySong[key].volume, s.volume);
    bySong[key].note = mergeNote(bySong[key].note, s.note);
  }

  return Object.keys(bySong)
    .map(k => bySong[k])
    .sort((a, b) => (firstCode(a) || a.title).localeCompare(firstCode(b) || b.title));
}

function chooseCode(current, incoming) {
  current = clean(current);
  incoming = clean(incoming);
  if (!current) return incoming;
  if (!incoming) return current;
  return incoming.localeCompare(current) < 0 ? incoming : current;
}

function mergeListText(a, b) {
  a = clean(a);
  b = clean(b);
  if (!a) return b;
  if (!b || a.split(";").map(x => clean(x)).includes(b)) return a;
  return a + "; " + b;
}

function rowToSongByHeader(headers, row) {
  function get(names) {
    for (let n of names) {
      let key = normalizeHeader(n);
      let idx = headers.indexOf(key);
      if (idx >= 0) return clean(row[idx]);
    }
    return "";
  }

  let title = get(["歌名", "歌曲名稱", "曲名", "Title", "Song"]);
  let singer = get(["歌手", "演唱者", "Artist", "Singer"]);
  let lang = get(["語言", "Language"]);
  let songNo = get(["歌號", "歌曲編號", "編號", "SongNumber", "Song No"]);
  let yinyuan = get(["音圓代號", "音圓", "InYuan", "Yinyuan"]);
  let jinsang = get(["金嗓代號", "金嗓", "GoldenVoice", "Jinsang"]);
  let hongyin = get(["弘音代號", "弘音", "Hongyin"]);
  let youtube = get(["YouTube", "Youtube", "YT"]);
  let note = get(["備註", "Note", "Memo"]);
  let volume = get(["集數", "Volume"]);

  // WPF 新格式：歌號,歌名,歌手,語言,音圓代號,集數
  // 這裡的「歌號」就是 KTV 歌號，放到 Scriptable 的音圓欄位，才搜尋得到。
  if (songNo && (!yinyuan || yinyuan === "音圓" || yinyuan === "Unknown")) {
    if (yinyuan && yinyuan !== "音圓" && yinyuan !== "Unknown") note = mergeNote(note, yinyuan);
    yinyuan = songNo;
  }

  return {
    title: title,
    singer: singer,
    lang: lang,
    yinyuan: yinyuan,
    jinsang: jinsang,
    hongyin: hongyin,
    youtube: youtube,
    note: note,
    volume: volume
  };
}

function parseCSV(text) {
  text = stripBom(text || "").replace(/\r\n/g, "\n").replace(/\r/g, "\n");
  let rows = [];
  let row = [];
  let cell = "";
  let quoted = false;

  for (let i = 0; i < text.length; i++) {
    let ch = text[i];
    if (quoted) {
      if (ch === "\"") {
        if (text[i + 1] === "\"") {
          cell += "\"";
          i++;
        } else {
          quoted = false;
        }
      } else {
        cell += ch;
      }
    } else {
      if (ch === "\"") quoted = true;
      else if (ch === ",") {
        row.push(cell);
        cell = "";
      } else if (ch === "\n") {
        row.push(cell);
        rows.push(row);
        row = [];
        cell = "";
      } else {
        cell += ch;
      }
    }
  }

  if (cell.length > 0 || row.length > 0) {
    row.push(cell);
    rows.push(row);
  }
  return rows;
}

function csvCell(v) {
  v = String(v == null ? "" : v);
  if (v.includes("\"") || v.includes(",") || v.includes("\n") || v.includes("\r")) {
    return "\"" + v.replace(/"/g, "\"\"") + "\"";
  }
  return v;
}

function normalizeHeader(v) {
  return clean(v).replace(/\s+/g, "").toLowerCase();
}

function clean(v) {
  return String(v == null ? "" : v).replace(/^\uFEFF/, "").trim();
}

function stripBom(v) {
  return String(v || "").replace(/^\uFEFF/, "");
}

function mergeNote(a, b) {
  a = clean(a);
  b = clean(b);
  if (!a) return b;
  if (!b) return a;
  if (a.indexOf(b) >= 0) return a;
  return a + "；" + b;
}

function firstCode(s) {
  return s.yinyuan || s.jinsang || s.hongyin || "";
}

function songKey(s) {
  return [
    s.title || "",
    s.singer || "",
    s.yinyuan || "",
    s.jinsang || "",
    s.hongyin || ""
  ].join("|").toLowerCase();
}

function dedupeSongs(list) {
  let seen = {};
  let out = [];
  for (let s of list) {
    let k = songKey(s);
    if (!seen[k]) {
      seen[k] = true;
      out.push(s);
    }
  }
  return out;
}

function isFav(s) {
  let k = songKey(s);
  return favorites.some(x => songKey(x) === k);
}

function toggleFavorite(s) {
  let k = songKey(s);
  if (isFav(s)) favorites = favorites.filter(x => songKey(x) !== k);
  else favorites.unshift(s);
  favorites = dedupeSongs(favorites);
  saveJson(favPath, favorites);
}

function recordPlay(s) {
  let k = (s.title || "未命名") + " / " + (s.singer || "");
  stats[k] = (stats[k] || 0) + 1;
  saveJson(statsPath, stats);
}

function replaceInList(list, oldKey, newSong, path) {
  let replaced = list.map(x => songKey(x) === oldKey ? newSong : x);
  saveJson(path, dedupeSongs(replaced));
}

function loadJson(path, fallback) {
  try {
    if (!fm.fileExists(path)) return fallback;
    return JSON.parse(fm.readString(path));
  } catch (e) {
    return fallback;
  }
}

function saveJson(path, value) {
  fm.writeString(path, JSON.stringify(value, null, 2));
}

function migrateLegacyData() {
  try {
    if (!iCloudFM.fileExists(legacyDataDir)) return;
    let names = [
      "master.csv",
      "local_songs.csv",
      "github_local_songs.csv",
      "favorites.json",
      "queue.json",
      "recent.json",
      "stats.json",
      "settings.json",
      "deleted.json"
    ];

    for (let name of names) {
      let src = iCloudFM.joinPath(legacyDataDir, name);
      let dst = fm.joinPath(dataDir, name);
      if (!iCloudFM.fileExists(src) || fm.fileExists(dst)) continue;
      fm.writeString(dst, iCloudFM.readString(src));
    }
  } catch (e) {
    // Migration is best-effort. New writes use the iPhone local folder.
  }
}

async function prompt(title, message, value) {
  let a = new Alert();
  a.title = title;
  a.message = message;
  a.addTextField(message, value || "");
  a.addAction("確定");
  a.addCancelAction("取消");
  let r = await a.presentAlert();
  if (r < 0) return null;
  return a.textFieldValue(0);
}

async function msg(title, message) {
  let a = new Alert();
  a.title = title;
  a.message = message;
  a.addAction("確定");
  await a.presentAlert();
}

function escapeHtml(v) {
  return String(v == null ? "" : v)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

await loadSongs();
await mainMenu();

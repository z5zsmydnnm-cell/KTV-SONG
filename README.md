# KTV-SONG 歌庫資料庫

這個 Repository 是 **KTV-V5 / Scriptable 點歌系統** 使用的雲端歌庫。

用途：

- 儲存音圓、金嗓、弘音等伴唱機歌單資料
- 提供 iPhone Scriptable / 捷徑自動下載更新
- 使用 `version.json` 判斷是否有新版歌單
- 之後可用 GitHub 直接維護與更新歌單

---

## 目錄說明

```text
KTV-SONG
│
├── README.md
├── version.json
├── songs/
│   ├── master.csv
│   ├── 音圓.csv
│   ├── 金嗓.csv
│   └── 弘音.csv
│
├── update/
│   └── 2026-07.csv
│
└── docs/
    └── csv-format.md
```

---

## 歌單 CSV 格式

建議固定使用 UTF-8 編碼。

```csv
歌名,歌手,語言,音圓代號,金嗓代號,弘音代號,YouTube連結,備註
愛情限時批,伍佰,台語,12345,0,0,,範例
童話,光良,國語,23456,34567,0,,範例
```

---

## Scriptable 更新網址

```text
https://raw.githubusercontent.com/z5zsmydnnm-cell/KTV-SONG/main/version.json
```

Scriptable 先讀取 `version.json`，再下載 `files` 裡面的 CSV。

---

## 維護方式

每次更新歌單時：

1. 上傳新的 CSV 到 `songs/` 或 `update/`
2. 修改 `version.json`
3. 版本號建議使用日期，例如 `2026.07.09`

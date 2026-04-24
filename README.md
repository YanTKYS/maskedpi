# MaskedPi (WPF ローカル個人情報マスキング補助ツール)

地方自治体職員が生成AIへ貼り付ける前に、個人情報・自治体固有IDをルールベースで簡易マスキングするローカル補助ツールです。

## v0.1.0 の位置づけ

`v0.1.0` は **プロトタイプ / 実機確認版** です。

- WPF ローカル実行
- JSON 設定（rules / dictionary / test cases）
- ルールテスター
- 業務別プロファイル切替
- GitHub Actions 手動ビルド / 手動リリース
- **self-contained 配布（win-x64）**

---

## self-contained 配布について（重要）

本リポジトリの GitHub Actions 配布物は、`MaskedPi.App` を **self-contained** で publish します。

- 対象: `win-x64`
- 目的: 対象端末に `.NET Desktop Runtime` が未導入でも起動できるようにする
- 方針: 初回は安定性優先（単一ファイル化・trim は無効）

### framework-dependent から変更した理由

対象端末で `.NET Desktop Runtime` が未導入の場合、framework-dependent 配布では起動時に runtime インストール要求が出るためです。

> `txt2voice` など他ツールが同端末で動作するケースは、実行ファイルに runtime を同梱した配布方式（self-contained）を採用している可能性があります。

### 注意

- self-contained 配布は成果物サイズが大きくなります（想定内）。
- 初回は `win-x64` 固定です。将来 `win-x86` 等を追加可能な構成です。

---

## 業務別プロファイル機能

利用可能プロファイル:

- `common`
- `resident`
- `tax`
- `welfare`
- `education`

実行時は **`common` + 選択プロファイル** のルールを適用します。

- `profile` 未指定ルールは `common` 扱い
- 誤検出が多い場合は、業務固有ルールを各 profile に分離

---

## フォルダ構成（抜粋）

```text
.github/
  workflows/
    manual-build.yml
    manual-release.yml
scripts/
  parse-release-notes.ps1
release-notes/
  v0.1.0.md
config/
  rules.json
  local_dictionary.json
  test_cases.json
src/
  MaskedPi.App/
  MaskedPi.Core/
tests/
  MaskedPi.Core.Tests/
Directory.Build.props
README.md
```

---

## バージョン管理

`Directory.Build.props` で .NET の内部バージョンを一元管理しています。

- .NET Version: `0.1.0`
- Git tag: `v0.1.0`

> 先頭 `v` の有無で用途が異なります（.NET は数値、GitHub Release は慣例で `v` 付き）。

---

## GitHub Actions: 通常ビルド（手動）

ワークフロー: **`Manual Build`** (`.github/workflows/manual-build.yml`)

### 使い方

1. GitHub の **Actions** タブを開く
2. `Manual Build` を選択
3. `Run workflow` を実行

### 実行内容

1. checkout
2. setup-dotnet (8.0.x)
3. restore
4. build (Release)
5. publish (`win-x64`, `self-contained=true`)
6. zip 化
7. artifact upload

### 成果物

- artifact 名: `maskedpi-build-win-x64-selfcontained-v0.1.0`
- ZIP をダウンロードし展開後、`MaskedPi.App.exe` を起動

---

## GitHub Actions: リリースビルド（手動）

ワークフロー: **`Manual Release Build`** (`.github/workflows/manual-release.yml`)

### 入力値

- `version`（既定: `v0.1.0`）
- `release_notes_path`（既定: `release-notes/v0.1.0.md`）

### 実行内容

1. checkout（tag チェックのため履歴取得）
2. setup-dotnet (8.0.x)
3. 既存 tag 存在チェック（存在したら失敗）
4. release-notes を PowerShell で解析
5. restore
6. build
7. publish (`win-x64`, `self-contained=true`)
8. zip 化（`maskedpi-vX.Y.Z-win-x64-selfcontained.zip`）
9. GitHub Release 作成
10. zip 添付

### 安全策

- push では起動しません（`workflow_dispatch` のみ）
- 既存 tag がある場合はリリースを作成しません（上書き防止）
- リリースノート形式が壊れていれば途中で明示的に失敗します

---

## release-notes 運用ルール

配置:

- `release-notes/v0.1.0.md`
- 将来: `release-notes/v0.1.1.md`, `release-notes/v0.2.0.md` ...

### 書式

- 1行目: H1 (`# ...`) → Release title
- 2行目以降: Release body

---

## 実機確認手順（runtime 未導入端末を想定）

1. Actions の成果物または Release Asset の zip を取得
2. 任意フォルダへ展開
3. `MaskedPi.App.exe` を起動
4. アプリ起動後、設定読込ステータスを確認

> 設定ファイルは実行フォルダ配下 `config/` を読みます（`AppContext.BaseDirectory` 基準）。

---

## ローカル実行（Windows / 開発用）

```powershell
cd src/MaskedPi.App
dotnet run
```

---

## 保守メモ

- 新規ルール追加時は `test_cases.json` に回帰ケースを追加
- profile 追加時は `MaskingService.SupportedProfiles` と辞書 `ByProfile` を更新
- 誤検出時は priority と profile 分離を先に見直す

---

## TODO

- 前回選択プロファイルの永続化
- test_cases の UI 一括実行
- profile 別統計の可視化
- `highRiskFreeTextKeywords` の注意喚起UI
- 将来オプションとして single-file 配布の評価（今回未対応）

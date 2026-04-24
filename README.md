# MaskedPi (WPF 最小実装)

地方自治体職員が生成AIへ貼り付ける前に、個人情報・自治体固有IDをルールベースで簡易マスキングするローカル補助ツールです。

## 実装方針（最小実装）

- **目的特化**: 「完全防止」ではなく、貼り付け前のワンクッションを短時間で提供。
- **閉域前提**: 外部APIなし、ローカルファイル（`rules.json` / `local_dictionary.json`）のみ使用。
- **保守性重視**: ルールはJSONで外部化し、コード修正なしで追加・無効化可能。
- **誤検出抑制**: 氏名・住所は保守的なルールを採用し、番号系・ラベル付き項目を優先。
- **分離設計**: UI (`MaskedPi.App`) と検出ロジック (`MaskedPi.Core`) を分離。

## 採用技術

- C# / .NET 8
- WPF (Windowsデスクトップ)
- `System.Text.Json`（設定読込）
- `Regex`（タイムアウト付き）

## フォルダ構成

```text
maskedpi/
├─ src/
│  ├─ MaskedPi.App/
│  │  ├─ App.xaml
│  │  ├─ MainWindow.xaml
│  │  └─ MainWindow.xaml.cs
│  └─ MaskedPi.Core/
│     ├─ RuleDefinition.cs
│     ├─ RuleLoader.cs
│     ├─ RuleEngine.cs
│     ├─ MaskingService.cs
│     ├─ MaskingResult.cs
│     ├─ ReplacementRecord.cs
│     ├─ DictionaryProvider.cs
│     └─ LocalDictionary.cs
├─ config/
│  ├─ rules.json
│  └─ local_dictionary.json
└─ README.md
```

## 主要クラス

- `MainWindow`
  - 入出力UI、ボタン操作（実行・コピー・クリア・設定再読込）、件数サマリ表示。
- `MaskingService`
  - ルール/辞書読込の統合、実行用ランタイムルール構築。
- `RuleLoader`
  - `rules.json` を読み込み、無効ルールをスキップして警告化。
- `DictionaryProvider`
  - `local_dictionary.json` の読込。
- `RuleEngine`
  - 優先順位順に候補抽出し、重複範囲は先勝ちで1回だけ置換。
- `MaskingResult` / `ReplacementRecord`
  - 置換結果、カテゴリ別件数、各置換詳細の保持。

## 実行方法（Windows）

```powershell
# 1) .NET 8 SDK インストール済み前提
# 2) WPF アプリを実行
cd src/MaskedPi.App
dotnet run
```

> `config/rules.json` と `config/local_dictionary.json` は実行時に自動読込されます。

## サンプル入出力

### 入力例

```text
申請者: 佐藤 太郎
電話 09012345678
メール test.user@example.com
住所: 東京都千代田区1-2-3
宛名番号: ATN-001122
生年月日: 昭和60年1月2日
```

### 出力例

```text
[氏名]
電話 [電話番号]
メール [メール]
住所: [住所]
[宛名番号]
生年月日: [日付]
```

## TODO（不足機能）

- 詳細なルール編集UI（現状はJSON直接編集）。
- `nameLabels` を使った動的ラベル氏名ルール生成（現状は `rules.json` 側で対応）。
- 部署名 + 氏名の複合ルール（辞書を使った安全な誤検出抑制設計が必要）。
- 置換履歴のCSV出力。
- 単体テストプロジェクトの追加（環境依存なく実行可能なCI設計）。

## 今後の拡張案

1. ルールテスター画面（正規表現とサンプル文で即時検証）。
2. ルールセットの業務別プロファイル切替（福祉/税務/教育など）。
3. 監査ログ（誰がいつ何件マスクしたかの匿名統計）。
4. ルールエンジンの別プロセス化（将来のAPI化を見据えた境界分離）。
5. 外部検出エンジンへの差し替えインターフェース（Presidio等）。

## テスト観点一覧

- 起動時に設定ファイル読込できるか（正常/ファイル欠損/JSON破損）。
- ルール順序（ローカル→電話→メール→郵便→日付→住所→氏名）が効くか。
- 重複マッチが二重置換されないか。
- 長文（数万文字）で実用速度を維持できるか。
- 誤検出が過剰でないか（特に住所/氏名）。
- コピー/クリア/UI表示が直感的に使えるか。


# MaskedPi (WPF ローカル個人情報マスキング補助ツール)

地方自治体職員が生成AIへ貼り付ける前に、個人情報・自治体固有IDをルールベースで簡易マスキングするローカル補助ツールです。

## 実装方針（検証強化版）

- **設定主導**: 固定ルールは `config/rules.json`、自治体差分は `config/local_dictionary.json` を編集。
- **検証重視**: ルール追加より先に、ルールテスターでヒット根拠を可視化。
- **責務分離**: UI (`MaskedPi.App`) は表示中心、検出・検証ロジックは `MaskedPi.Core` に集約。
- **将来拡張**: `profile` / `source` / `notes` / `tags` を保持し、業務別運用に備える。

## 追加機能（今回）

- WPF に **ルールテスタータブ** を追加。
  - テスト入力
  - テスト実行
  - マスキング後出力
  - ルールヒット詳細一覧（RuleName / Category / Priority / MatchedText / Replacement / StartIndex / Length / Source）
  - 設定再読込
  - 出力コピー
  - 詳細結果コピー（TSV）
- `ReplacementRecord` を拡張し、`Priority` / `Source` / `Notes` を保持。
- `MaskingResult` に `RuleHitCounts` を追加。
- `RuleTestCase` / `RuleTestCaseLoader` / `RuleTestRunner` を追加。
- `config/test_cases.json` を追加し、JSONベースの検証ケース管理を導入。

## フォルダ構成

```text
maskedpi/
├─ src/
│  ├─ MaskedPi.App/
│  └─ MaskedPi.Core/
├─ tests/
│  └─ MaskedPi.Core.Tests/
├─ config/
│  ├─ rules.json
│  ├─ local_dictionary.json
│  └─ test_cases.json
└─ README.md
```

## ルールテスター画面の使い方

1. 「ルールテスター」タブを開く。
2. テストしたい文章を入力。
3. 「テスト実行」をクリック。
4. 出力欄でマスキング結果を確認。
5. 下段の「ヒット詳細」で、どのルールが何を置換したか確認。
6. 必要に応じて「詳細結果をコピー」でレビュー用に共有。

## ルール追加後の確認手順（推奨）

1. `rules.json` / `local_dictionary.json` を更新。
2. アプリで「設定再読込」。
3. ルールテスターで代表入力を実行。
4. ヒット詳細で `RuleName` / `Priority` / `Source` を確認。
5. `config/test_cases.json` に再発防止ケースを追加。
6. `dotnet test` を実行して回帰確認。

## test_cases.json の書き方

`config/test_cases.json` は以下構造です。

- `caseName`: ケース識別名
- `input`: 入力文
- `expectedOutput`: 期待出力
- `expectedHitRules`: 期待ヒットルール名配列
- `description`: 意図
- `profile`: 任意（common/resident/tax/welfare/education など）

例:

```json
{
  "caseName": "basic_phone",
  "input": "電話番号 090-1234-5678",
  "expectedOutput": "電話番号 [電話番号]",
  "expectedHitRules": ["Phone-General"],
  "description": "電話番号",
  "profile": "common"
}
```

## 自治体ローカルルールを増やすときの注意点

- まずラベル付き（例: `相談管理番号:`）を追加。
- 次にプレフィックス（例: `CASE-`）を追加。
- 汎用ルール（`番号` / `ID`）は最後に追加し、priority は後段へ。
- 追加時は必ず誤検出ケースを `test_cases.json` に同時登録。

## 誤検出が起きたときの見直し手順

1. ルールテスターでヒット詳細を確認（どのルールが原因か特定）。
2. ルールの `pattern` を狭める（文字数・前後文脈・ラベル必須化）。
3. `priority` を下げるか、より具体的なルールを前段へ。
4. 部署+氏名系は除外語（御中/各位/様/主査 等）を見直す。
5. 再発防止ケースを `test_cases.json` に追加。

## priority 設計指針

- 10〜29: LocalRule
- 30〜39: ラベル付き属性（辞書動的ルール）
- 40〜49: Phone / Email / PostalCode
- 50〜59: Date
- 60〜69: Address
- 70〜79: Name
- 80〜99: 補助ルール

`RuleEngine` は重複範囲を先勝ちで確定するため、priority は必ず明示設計してください。

## 推奨保守フロー

1. 週次で誤検出/漏れ事例を収集。
2. `local_dictionary.json` でまず対応。
3. それでも不足する場合のみ `rules.json` を追加。
4. `test_cases.json` へケース追加。
5. `dotnet test` 実行。
6. 反映後はルールテスターで最終確認。

## テスト観点（実装済み）

- `rules.json` 正常読込
- `local_dictionary.json` 正常読込
- JSONキー欠落時の継続
- 無効ルールのスキップ
- priority 先勝ち
- 同一範囲の重複置換抑止
- ローカルルール優先
- `nameLabels` 由来ルール生成
- `departmentNames + 氏名` の誤検出抑止
- 長文入力で例外が出ない
- `test_cases.json` ロード

## 実行方法（Windows）

```powershell
cd src/MaskedPi.App
dotnet run
```

## TODO

- ルールテスターの「test_cases.json 読込→一括実行」UI。
- profile 切替UI。
- `highRiskFreeTextKeywords` の注意喚起表示（非置換）。
- CI で `dotnet test` 自動実行。

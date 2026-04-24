# MaskedPi (WPF ローカル個人情報マスキング補助ツール)

地方自治体職員が生成AIへ貼り付ける前に、個人情報・自治体固有IDをルールベースで簡易マスキングするローカル補助ツールです。

## 実装方針（業務別プロファイル対応版）

- **設定主導**: 固定ルールは `config/rules.json`、自治体差分は `config/local_dictionary.json` を編集。
- **検証重視**: ルールテスターでヒット根拠を可視化。
- **業務別最適化**: `common` + 選択プロファイルのみ適用し、過剰置換を抑制。
- **責務分離**: UI (`MaskedPi.App`) は表示中心、検出/選別ロジックは `MaskedPi.Core` に集約。

## 業務別プロファイル機能の概要

利用可能プロファイル:

- `common`
- `resident`
- `tax`
- `welfare`
- `education`

実行時は **`common` + 選択プロファイル** のルールが有効になります。

- 例: `tax` を選ぶと `common` と `tax` のみ適用
- `profile` 未指定ルールは `common` 扱い

## UI（最小追加）

- マスキングタブ: 「業務プロファイル」コンボボックスを追加
- ルールテスタータブ: 「テスタープロファイル」コンボボックスを追加

どちらも同じプロファイル集合を使用し、再実行しやすい構成です。

## 追加機能（今回）

- プロファイル選択に応じた有効ルール抽出（`MaskingService.GetEffectiveProfileRules`）。
- 動的辞書ルールのプロファイル対応（`DepartmentNamesByProfile` / `LocalLabelsByProfile` / `FacilityNamesByProfile`）。
- `RuleTestRunner` がケースごとの `profile` を使って実行可能。

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

## rules.json での profile の書き方

```json
{
  "name": "TaxNotificationNumber",
  "category": "LocalRule",
  "pattern": "納税通知書番号[:：]?\\s*[0-9A-Za-z\\-]+",
  "replacement": "[納税通知書番号]",
  "priority": 15,
  "enabled": true,
  "profile": "tax",
  "description": "税務系の通知番号"
}
```

- `profile` 省略時は実行時に `common` として扱われます。
- 将来の複数所属に備え、内部では拡張しやすい評価構造にしています。

## local_dictionary.json の ByProfile 構造例

```json
{
  "departmentNamesByProfile": {
    "welfare": ["生活福祉課", "障害福祉課", "高齢者支援課"],
    "tax": ["税務課", "納税課"],
    "education": ["学校教育課", "学務課"]
  },
  "localLabelsByProfile": {
    "welfare": ["ケース番号", "相談管理番号"],
    "tax": ["納税通知書番号", "調定番号"],
    "education": ["学籍番号", "学校名簿番号"]
  },
  "facilityNamesByProfile": {
    "education": ["第一小学校", "第二中学校"],
    "welfare": ["福祉センター"]
  }
}
```

キーが無くてもロードは失敗しません。

## どの業務でどのプロファイルを選ぶか

- 住民記録・窓口: `resident`
- 税務・収納: `tax`
- 福祉・介護・相談: `welfare`
- 学校・学籍: `education`
- 横断文書・不明時: `common`

## 誤検出抑制のためにプロファイルを分ける考え方

- まず `common` は最小限（電話・メール・郵便・日付など）に保つ。
- 業務固有IDは対応プロファイルへ寄せる。
- 誤検出時は、
  1. ルールを具体化
  2. プロファイルを見直し
  3. `test_cases.json` に再発防止ケース追加

## ルールテスター画面の使い方

1. 「ルールテスター」タブを開く。
2. テスタープロファイルを選択。
3. テスト文章を入力して実行。
4. ヒット詳細（RuleName/Priority/Source）を確認。

## ルール追加後の確認手順（推奨）

1. `rules.json` / `local_dictionary.json` を更新
2. アプリで設定再読込
3. 対象プロファイルでマスキング実行
4. ルールテスターで詳細確認
5. `test_cases.json` に回帰ケース追加
6. `dotnet test` 実行

## test_cases.json の書き方

- `caseName`
- `input`
- `expectedOutput`
- `expectedHitRules`
- `description`
- `profile`（任意）

## テスト観点（実装済み）

- `common` のみ適用されること
- `tax` で `common + tax` が有効なこと
- `welfare` で `tax` ルールが混ざらないこと
- `ByProfile` 辞書キー読込
- `profile` 未指定ルールの `common` 扱い
- プロファイル切替で同一入力結果が変わること
- RuleTestRunner でケース profile が反映されること

## 実行方法（Windows）

```powershell
cd src/MaskedPi.App
dotnet run
```

## TODO

- 前回選択プロファイルの永続化。
- test_cases の UI 一括実行。
- プロファイル別の統計表示。
- `highRiskFreeTextKeywords` の注意喚起表示。

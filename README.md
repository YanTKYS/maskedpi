# MaskedPi (WPF ローカル個人情報マスキング補助ツール)

地方自治体職員が生成AIへ貼り付ける前に、個人情報・自治体固有IDをルールベースで簡易マスキングするローカル補助ツールです。

## 実装方針（改善版）

- **設定主導**: 固定ルールは `config/rules.json`、自治体差分は `config/local_dictionary.json` を編集して育成。
- **誤検出抑制**: ラベル付き表現を優先し、曖昧な自由文検出は保守的に扱う。
- **責務分離**: UI (`MaskedPi.App`) と検出ロジック (`MaskedPi.Core`) を分離。
- **将来拡張**: `profile`・`source`・`tags` を持つルール設計で業務別管理を見据える。

## 主な追加点

- `rules.json` を大幅拡充（自治体ID系、日付、連絡先、住所、補助ルール）。
- `local_dictionary.json` を拡充（labels / facilities / areas / idSuffixKeywords / highRiskFreeTextKeywords）。
- `MaskingService` に辞書ベース動的ルール生成を実装：
  - `BuildLocalLabelRules`
  - `BuildNameLabelRules`
  - `BuildAddressLabelRules`
  - `BuildDateLabelRules`
  - `BuildContactLabelRules`
  - `BuildDepartmentPersonRules`
  - `BuildDictionaryFullNameRules`
- 除外語（御中・各位・様 等）を導入し部署名＋氏名ルールの過検出を抑制。

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
│  └─ local_dictionary.json
└─ README.md
```

## ルール追加方法（rules.json）

1. `config/rules.json` の `rules` 配列にオブジェクトを追加。
2. 最低限、以下を設定：
   - `name`, `enabled`, `category`, `pattern`, `replacement`, `priority`
3. 推奨で以下も設定：
   - `profile`, `description`, `sampleText`, `notes`, `source`, `tags`
4. ルール優先順位の目安：
   - 10〜29: LocalRule
   - 30〜39: ラベル付き属性（主に辞書動的ルール）
   - 40〜49: Phone / Email / PostalCode
   - 50〜59: Date
   - 60〜69: Address
   - 70〜79: Name
   - 80〜99: 補助ルール

## 辞書追加方法（local_dictionary.json）

1. `config/local_dictionary.json` のキーに値を追加。
2. 特に更新頻度が高いのは以下：
   - `nameLabels`, `addressLabels`, `dateLabels`, `contactLabels`
   - `localLabels`, `idPrefixes`, `idSuffixKeywords`
   - `departmentNames`
3. キー欠落時は空配列で扱う設計のため、段階的追加が可能。

## 自治体ローカルルールの育て方

- まずは帳票で明示的なラベル（例: `相談管理番号:`）を優先して追加。
- 次にプレフィックス（例: `CASE-`）を追加。
- それでも漏れる場合にのみ汎用ルール（末尾キーワード `番号` など）を追加。
- 誤検出が増えたら優先度を下げるか、正規表現を厳しくする。

## 誤検出を抑えるコツ

- 氏名・住所の自由文抽出は控えめにし、ラベル付き検出を優先する。
- 部署名 + 氏名ルールは除外語（御中/各位/様 等）を必ず適用する。
- `.{0,40}` のような曖昧範囲は短めに保つ。
- 「検出漏れを少し許容して誤検出を減らす」バランスを基本にする。

## 優先順位設計の考え方

- 個別性の高いルールを先（低い priority）に置く。
- 汎用的な補助ルールは後ろ（高い priority）へ配置。
- RuleEngine は重複範囲を先勝ちで確定するため、priority が挙動を決定する。

## 業務別プロファイル構想

- 現時点では UI 切替は未実装。
- ただし、各ルールに `profile` を保持済み：
  - `common`, `resident`, `tax`, `welfare`, `education`
- 今後は profile 単位で読込フィルタを追加するだけで業務別運用へ移行可能。

## サンプル入力 / 出力（追加例）

### 入力

```text
福祉課 担当 山田花子
相談管理番号: SDN-5566
生年月日: 昭和60年1月2日
電話番号: 090-1234-5678
送付先住所: 東京都千代田区1-2-3
```

### 出力

```text
[部署担当者]
[相談管理番号]
[日付]
[連絡先]
[住所]
```

## テスト

`tests/MaskedPi.Core.Tests` に以下の観点を追加済みです。

- 辞書から `nameLabels` を使った動的氏名ルール生成。
- 辞書から `addressLabels` / `dateLabels` / `contactLabels` ルール生成。
- `localLabels` と `idPrefixes` のルール生成。
- 競合時の priority 先勝ち。
- `departmentNames + 氏名` の過検出抑制（除外語）。
- JSON キー欠落時の読込継続。

## 実行方法（Windows）

```powershell
cd src/MaskedPi.App
dotnet run
```

## TODO

- profile 切替UIの追加。
- `facilityNames` / `schoolNames` / `localAreas` を使う動的ルール拡張。
- `highRiskFreeTextKeywords` の注意喚起表示（非置換）の実装。
- ルール編集・テスト画面の追加。
- CI で `dotnet test` 自動実行。

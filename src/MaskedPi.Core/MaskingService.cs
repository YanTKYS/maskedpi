using System.Text.RegularExpressions;

namespace MaskedPi.Core;

/// <summary>
/// UI から利用するアプリケーションサービス。
/// ルールと辞書を統合し、最終的なマスキングを実行する。
/// </summary>
public sealed class MaskingService
{
    private readonly RuleLoader _ruleLoader;
    private readonly DictionaryProvider _dictionaryProvider;
    private readonly RuleEngine _ruleEngine;

    public MaskingService(RuleLoader ruleLoader, DictionaryProvider dictionaryProvider, RuleEngine ruleEngine)
    {
        _ruleLoader = ruleLoader;
        _dictionaryProvider = dictionaryProvider;
        _ruleEngine = ruleEngine;
    }

    public RuntimeLoadResult Load(string rulePath, string dictionaryPath)
    {
        var ruleResult = _ruleLoader.Load(rulePath);
        var dictResult = _dictionaryProvider.Load(dictionaryPath);

        if (!ruleResult.Success)
        {
            return RuntimeLoadResult.Fail(ruleResult.ErrorMessage ?? "ルール読み込み失敗");
        }

        if (!dictResult.Success)
        {
            return RuntimeLoadResult.Fail(dictResult.ErrorMessage ?? "辞書読み込み失敗");
        }

        var runtimeRules = BuildRuntimeRules(ruleResult.Rules, dictResult.Dictionary);
        var warnings = new List<string>(ruleResult.Warnings);

        return new RuntimeLoadResult(true, runtimeRules, warnings, null);
    }

    public MaskingResult Mask(string input, IReadOnlyCollection<RuleDefinition> rules)
    {
        return _ruleEngine.Apply(input, rules);
    }

    private static List<RuleDefinition> BuildRuntimeRules(List<RuleDefinition> baseRules, LocalDictionary dictionary)
    {
        var runtime = new List<RuleDefinition>(baseRules);

        // 辞書のローカルラベルから自動的に番号系ルールを補完する。
        foreach (var label in dictionary.LocalLabels.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        {
            runtime.Add(new RuleDefinition
            {
                Name = $"LocalLabel::{label}",
                Enabled = true,
                Category = RuleCategory.LocalRule,
                Pattern = $"{Regex.Escape(label)}[:：]?\\s*[0-9A-Za-z\\-]+",
                Replacement = $"[{label}]",
                Priority = 10,
                Description = "辞書由来のローカルラベル"
            });
        }

        // 氏名辞書は誤検出抑制のため、姓・名の組み合わせがある場合のみ有効化する。
        if (dictionary.Surnames.Count > 0 && dictionary.GivenNames.Count > 0)
        {
            var surnameAlt = string.Join("|", dictionary.Surnames.Select(Regex.Escape).Take(100));
            var givenAlt = string.Join("|", dictionary.GivenNames.Select(Regex.Escape).Take(100));
            runtime.Add(new RuleDefinition
            {
                Name = "DictionaryFullName",
                Enabled = true,
                Category = RuleCategory.Name,
                Pattern = $"(?:{surnameAlt})(?:\\s|　)?(?:{givenAlt})",
                Replacement = "[氏名]",
                Priority = 70,
                Description = "辞書由来の簡易氏名判定"
            });
        }

        return runtime
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();
    }
}

public sealed record RuntimeLoadResult(bool Success, List<RuleDefinition> Rules, List<string> Warnings, string? ErrorMessage)
{
    public static RuntimeLoadResult Fail(string message) => new(false, new(), new(), message);
}

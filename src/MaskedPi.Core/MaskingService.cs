using System.Text.RegularExpressions;

namespace MaskedPi.Core;

/// <summary>
/// UI から利用するアプリケーションサービス。
/// ルールと辞書を統合し、最終的なマスキングを実行する。
/// </summary>
public sealed class MaskingService
{
    private static readonly string[] ExcludedNonNameWords =
    [
        "御中", "各位", "様", "殿", "一同", "担当", "係", "主査", "主任", "課長", "部長"
    ];

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
        runtime.AddRange(BuildLocalLabelRules(dictionary));
        runtime.AddRange(BuildNameLabelRules(dictionary));
        runtime.AddRange(BuildAddressLabelRules(dictionary));
        runtime.AddRange(BuildDateLabelRules(dictionary));
        runtime.AddRange(BuildContactLabelRules(dictionary));
        runtime.AddRange(BuildDepartmentPersonRules(dictionary));
        runtime.AddRange(BuildDictionaryFullNameRules(dictionary));

        return runtime
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<RuleDefinition> BuildLocalLabelRules(LocalDictionary dictionary)
    {
        foreach (var label in DistinctSafe(dictionary.LocalLabels))
        {
            yield return new RuleDefinition
            {
                Name = $"DynamicLocalLabel::{label}",
                Enabled = true,
                Category = RuleCategory.LocalRule,
                Pattern = $"{Regex.Escape(label)}[:：]?\\s*[0-9A-Za-z\\-]{3,}",
                Replacement = $"[{label}]",
                Priority = 12,
                Profile = "common",
                Source = "local-dictionary",
                Description = "辞書由来のローカル番号ラベル",
                SampleText = $"{label}: ABC-001122",
                Notes = "ラベル付き番号を優先検出"
            };
        }

        foreach (var prefix in DistinctSafe(dictionary.IdPrefixes))
        {
            yield return new RuleDefinition
            {
                Name = $"DynamicPrefixId::{prefix}",
                Enabled = true,
                Category = RuleCategory.LocalRule,
                Pattern = $"{Regex.Escape(prefix)}[0-9A-Za-z\\-]{{3,}}",
                Replacement = "[自治体ID]",
                Priority = 18,
                Profile = "common",
                Source = "local-dictionary",
                Description = "辞書由来のプレフィックス付きID",
                SampleText = $"{prefix}000123",
                Notes = "業務別IDプレフィックスに対応"
            };
        }

        foreach (var suffix in DistinctSafe(dictionary.IdSuffixKeywords))
        {
            yield return new RuleDefinition
            {
                Name = $"DynamicSuffixKeyword::{suffix}",
                Enabled = true,
                Category = RuleCategory.LocalRule,
                Pattern = $"[一-龥ぁ-んァ-ヶA-Za-z]{{1,12}}{Regex.Escape(suffix)}[:：]?\\s*[0-9A-Za-z\\-]{{3,}}",
                Replacement = "[識別子]",
                Priority = 28,
                Profile = "common",
                Source = "local-dictionary",
                Description = "辞書由来の末尾キーワード付き識別子",
                SampleText = $"照会{suffix}: RQ-7788",
                Notes = "誤検出抑制のため3文字以上の番号のみ"
            };
        }
    }

    private static IEnumerable<RuleDefinition> BuildNameLabelRules(LocalDictionary dictionary)
    {
        var labels = DistinctSafe(dictionary.NameLabels).ToList();
        if (labels.Count == 0)
        {
            yield break;
        }

        var labelAlt = BuildAlternation(labels);
        var excludedAlt = BuildAlternation(ExcludedNonNameWords);

        yield return new RuleDefinition
        {
            Name = "DynamicNameLabelRule",
            Enabled = true,
            Category = RuleCategory.Name,
            Pattern = $"(?:{labelAlt})[:：]\\s*(?!(?:{excludedAlt})\\b)[一-龥々ぁ-んァ-ヶ]{{1,12}}(?:[ 　][一-龥々ぁ-んァ-ヶ]{{1,12}})?",
            Replacement = "[氏名]",
            Priority = 35,
            Profile = "common",
            Source = "local-dictionary",
            Description = "辞書 nameLabels から生成するラベル付き氏名",
            SampleText = "申請者: 山田 太郎",
            Notes = "敬称や役職語は除外"
        };
    }

    private static IEnumerable<RuleDefinition> BuildAddressLabelRules(LocalDictionary dictionary)
    {
        var labels = DistinctSafe(dictionary.AddressLabels).ToList();
        if (labels.Count == 0)
        {
            yield break;
        }

        var labelAlt = BuildAlternation(labels);
        yield return new RuleDefinition
        {
            Name = "DynamicAddressLabelRule",
            Enabled = true,
            Category = RuleCategory.Address,
            Pattern = $"(?:{labelAlt})[:：]\\s*(?:東京都|北海道|(?:京都|大阪)府|..県).{{1,40}}",
            Replacement = "[住所]",
            Priority = 32,
            Profile = "common",
            Source = "local-dictionary",
            Description = "辞書 addressLabels から生成するラベル付き住所",
            SampleText = "現住所: 東京都千代田区1-2-3",
            Notes = "自由文住所より優先"
        };
    }

    private static IEnumerable<RuleDefinition> BuildDateLabelRules(LocalDictionary dictionary)
    {
        var labels = DistinctSafe(dictionary.DateLabels).ToList();
        if (labels.Count == 0)
        {
            yield break;
        }

        const string dateToken = "(?:\\d{4}[/-]\\d{1,2}[/-]\\d{1,2}|(?:令和|平成|昭和)(?:元|\\d{1,2})年\\d{1,2}月\\d{1,2}日)";
        var labelAlt = BuildAlternation(labels);

        yield return new RuleDefinition
        {
            Name = "DynamicDateLabelRule",
            Enabled = true,
            Category = RuleCategory.Date,
            Pattern = $"(?:{labelAlt})[:：]\\s*{dateToken}",
            Replacement = "[日付]",
            Priority = 33,
            Profile = "common",
            Source = "local-dictionary",
            Description = "辞書 dateLabels から生成するラベル付き日付",
            SampleText = "生年月日: 昭和60年1月2日",
            Notes = "和暦・西暦を同時に対応"
        };
    }

    private static IEnumerable<RuleDefinition> BuildContactLabelRules(LocalDictionary dictionary)
    {
        var labels = DistinctSafe(dictionary.ContactLabels).ToList();
        if (labels.Count == 0)
        {
            yield break;
        }

        var labelAlt = BuildAlternation(labels);
        const string phoneOrEmail = "(?:0\\d{1,4}-?\\d{1,4}-?\\d{3,4}|[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,})";

        yield return new RuleDefinition
        {
            Name = "DynamicContactLabelRule",
            Enabled = true,
            Category = RuleCategory.Other,
            Pattern = $"(?:{labelAlt})[:：]\\s*{phoneOrEmail}",
            Replacement = "[連絡先]",
            Priority = 34,
            Profile = "common",
            Source = "local-dictionary",
            Description = "辞書 contactLabels から生成するラベル付き連絡先",
            SampleText = "電話番号: 090-1234-5678",
            Notes = "電話とメールをまとめてマスク"
        };
    }

    private static IEnumerable<RuleDefinition> BuildDepartmentPersonRules(LocalDictionary dictionary)
    {
        var departments = DistinctSafe(dictionary.DepartmentNames).ToList();
        if (departments.Count == 0)
        {
            yield break;
        }

        var deptAlt = BuildAlternation(departments);
        var excludedAlt = BuildAlternation(ExcludedNonNameWords);

        yield return new RuleDefinition
        {
            Name = "DynamicDepartmentPersonRule",
            Enabled = true,
            Category = RuleCategory.Name,
            Pattern = $"(?:{deptAlt})\\s*(?:担当\\s*)?(?!(?:{excludedAlt})\\b)[一-龥々ぁ-んァ-ヶ]{{1,10}}(?:[ 　][一-龥々ぁ-んァ-ヶ]{{1,10}})?",
            Replacement = "[部署担当者]",
            Priority = 78,
            Profile = "common",
            Source = "local-dictionary",
            Description = "部署名 + 氏名の複合ルール",
            SampleText = "福祉課 担当 山田花子",
            Notes = "誤検出抑制のため1〜2語・最大10文字"
        };
    }

    private static IEnumerable<RuleDefinition> BuildDictionaryFullNameRules(LocalDictionary dictionary)
    {
        var surnames = DistinctSafe(dictionary.Surnames).Take(200).ToList();
        var givens = DistinctSafe(dictionary.GivenNames).Take(200).ToList();
        if (surnames.Count == 0 || givens.Count == 0)
        {
            yield break;
        }

        var surnameAlt = BuildAlternation(surnames);
        var givenAlt = BuildAlternation(givens);

        yield return new RuleDefinition
        {
            Name = "DynamicDictionaryFullNameRule",
            Enabled = true,
            Category = RuleCategory.Name,
            Pattern = $"(?:{surnameAlt})(?:\\s|　)?(?:{givenAlt})",
            Replacement = "[氏名]",
            Priority = 79,
            Profile = "common",
            Source = "local-dictionary",
            Description = "姓・名辞書の組合せによる保守的氏名判定",
            SampleText = "佐藤 太郎",
            Notes = "辞書外氏名は検出しない"
        };
    }

    private static IEnumerable<string> DistinctSafe(IEnumerable<string>? source)
        => (source ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal);

    private static string BuildAlternation(IEnumerable<string> values)
        => string.Join("|", values.Select(Regex.Escape));
}

public sealed record RuntimeLoadResult(bool Success, List<RuleDefinition> Rules, List<string> Warnings, string? ErrorMessage)
{
    public static RuntimeLoadResult Fail(string message) => new(false, new(), new(), message);
}

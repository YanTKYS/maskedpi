using System.Text.Json;

namespace MaskedPi.Core;

/// <summary>
/// ルール設定ファイルの読み込みを担当する。
/// 破損時でもアプリ全体が落ちないよう、失敗内容を返す。
/// </summary>
public sealed class RuleLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public RuleLoadResult Load(string path)
    {
        if (!File.Exists(path))
        {
            return RuleLoadResult.Fail($"ルールファイルが見つかりません: {path}");
        }

        try
        {
            var json = File.ReadAllText(path);
            var set = JsonSerializer.Deserialize<RuleSet>(json, JsonOptions);
            if (set is null)
            {
                return RuleLoadResult.Fail("ルールファイルの読み込み結果が空でした。");
            }

            var warnings = new List<string>();
            var validRules = new List<RuleDefinition>();
            foreach (var rule in set.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Name) || string.IsNullOrWhiteSpace(rule.Pattern))
                {
                    warnings.Add("名前またはパターンが空のルールをスキップしました。");
                    continue;
                }

                try
                {
                    var _ = new System.Text.RegularExpressions.Regex(
                        rule.Pattern,
                        rule.IgnoreCase ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None,
                        TimeSpan.FromMilliseconds(200));
                    validRules.Add(rule);
                }
                catch (Exception ex)
                {
                    warnings.Add($"ルール '{rule.Name}' をスキップ: {ex.Message}");
                }
            }

            return new RuleLoadResult(true, validRules, warnings, null);
        }
        catch (Exception ex)
        {
            return RuleLoadResult.Fail($"ルールファイル読み込みエラー: {ex.Message}");
        }
    }
}

public sealed record RuleLoadResult(
    bool Success,
    List<RuleDefinition> Rules,
    List<string> Warnings,
    string? ErrorMessage)
{
    public static RuleLoadResult Fail(string message) => new(false, new(), new(), message);
}

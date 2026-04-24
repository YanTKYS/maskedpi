using System.Text.Json;

namespace MaskedPi.Core;

public sealed class RuleTestCaseLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public RuleTestCaseLoadResult Load(string path)
    {
        if (!File.Exists(path))
        {
            return RuleTestCaseLoadResult.Fail($"テストケースファイルが見つかりません: {path}");
        }

        try
        {
            var json = File.ReadAllText(path);
            var set = JsonSerializer.Deserialize<RuleTestCaseSet>(json, JsonOptions) ?? new RuleTestCaseSet();
            return new RuleTestCaseLoadResult(true, set.Cases, null);
        }
        catch (Exception ex)
        {
            return RuleTestCaseLoadResult.Fail($"テストケース読み込みエラー: {ex.Message}");
        }
    }
}

public sealed record RuleTestCaseLoadResult(bool Success, List<RuleTestCase> Cases, string? ErrorMessage)
{
    public static RuleTestCaseLoadResult Fail(string message) => new(false, new(), message);
}

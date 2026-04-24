using System.Text.Json;

namespace MaskedPi.Core;

/// <summary>
/// 自治体ローカル辞書の読み込みを担当する。
/// </summary>
public sealed class DictionaryProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public DictionaryLoadResult Load(string path)
    {
        if (!File.Exists(path))
        {
            return DictionaryLoadResult.Fail($"辞書ファイルが見つかりません: {path}");
        }

        try
        {
            var json = File.ReadAllText(path);
            var dictionary = JsonSerializer.Deserialize<LocalDictionary>(json, JsonOptions) ?? new LocalDictionary();
            return new DictionaryLoadResult(true, dictionary, null);
        }
        catch (Exception ex)
        {
            return DictionaryLoadResult.Fail($"辞書ファイル読み込みエラー: {ex.Message}");
        }
    }
}

public sealed record DictionaryLoadResult(bool Success, LocalDictionary Dictionary, string? ErrorMessage)
{
    public static DictionaryLoadResult Fail(string message) => new(false, new LocalDictionary(), message);
}

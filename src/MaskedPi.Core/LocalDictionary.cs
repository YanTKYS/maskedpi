namespace MaskedPi.Core;

/// <summary>
/// 自治体ごとに編集可能な辞書定義。
/// </summary>
public sealed class LocalDictionary
{
    public List<string> NameLabels { get; set; } = new();
    public List<string> Surnames { get; set; } = new();
    public List<string> GivenNames { get; set; } = new();
    public List<string> DepartmentNames { get; set; } = new();
    public List<string> LocalLabels { get; set; } = new();
    public List<string> IdPrefixes { get; set; } = new();
}

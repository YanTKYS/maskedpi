namespace MaskedPi.Core;

/// <summary>
/// 自治体ごとに編集可能な辞書定義。
/// キー欠落時も空配列として扱えるよう、すべて初期化しておく。
/// </summary>
public sealed class LocalDictionary
{
    public List<string> NameLabels { get; set; } = new();
    public List<string> AddressLabels { get; set; } = new();
    public List<string> DateLabels { get; set; } = new();
    public List<string> ContactLabels { get; set; } = new();

    public List<string> Surnames { get; set; } = new();
    public List<string> GivenNames { get; set; } = new();

    public List<string> DepartmentNames { get; set; } = new();
    public List<string> FacilityNames { get; set; } = new();
    public List<string> SchoolNames { get; set; } = new();
    public List<string> LocalAreas { get; set; } = new();

    public List<string> LocalLabels { get; set; } = new();
    public List<string> IdPrefixes { get; set; } = new();
    public List<string> IdSuffixKeywords { get; set; } = new();

    public List<string> HighRiskFreeTextKeywords { get; set; } = new();
}

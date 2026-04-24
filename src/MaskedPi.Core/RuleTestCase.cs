namespace MaskedPi.Core;

/// <summary>
/// JSON で管理するルール検証ケース。
/// </summary>
public sealed class RuleTestCase
{
    public string CaseName { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public List<string> ExpectedHitRules { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string Profile { get; set; } = "common";
}

public sealed class RuleTestCaseSet
{
    public List<RuleTestCase> Cases { get; set; } = new();
}

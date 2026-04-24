namespace MaskedPi.Core;

/// <summary>
/// ルールの種別。表示やサマリ集計に利用する。
/// </summary>
public enum RuleCategory
{
    LocalRule,
    Phone,
    Email,
    PostalCode,
    Date,
    Address,
    Name,
    Other
}

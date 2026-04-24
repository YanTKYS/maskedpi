using System.Text.Json;
using MaskedPi.Core;

namespace MaskedPi.Core.Tests;

public sealed class MaskingServiceTests
{
    [Fact]
    public void Load_GeneratesNameAddressDateContactRules_FromDictionary()
    {
        var fixture = CreateFixture();
        var result = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);

        Assert.True(result.Success);
        Assert.Contains(result.Rules, r => r.Name == "DynamicNameLabelRule");
        Assert.Contains(result.Rules, r => r.Name == "DynamicAddressLabelRule");
        Assert.Contains(result.Rules, r => r.Name == "DynamicDateLabelRule");
        Assert.Contains(result.Rules, r => r.Name == "DynamicContactLabelRule");
    }

    [Fact]
    public void Load_GeneratesLocalLabelAndPrefixRules_FromDictionary()
    {
        var fixture = CreateFixture();
        var result = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);

        Assert.True(result.Success);
        Assert.Contains(result.Rules, r => r.Name.StartsWith("DynamicLocalLabel::", StringComparison.Ordinal));
        Assert.Contains(result.Rules, r => r.Name.StartsWith("DynamicPrefixId::", StringComparison.Ordinal));
    }

    [Fact]
    public void RulePriority_LocalRuleWins_WhenPatternsOverlap()
    {
        var fixture = CreateFixture();
        var result = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);
        Assert.True(result.Success);

        var input = "申請番号: SINSEI-1001";
        var masked = fixture.Service.Mask(input, result.Rules);

        Assert.Contains("[申請番号]", masked.MaskedText);
        Assert.DoesNotContain("[識別子]", masked.MaskedText);
    }

    [Fact]
    public void DepartmentPersonRule_AvoidsCommonNonNameWords()
    {
        var fixture = CreateFixture();
        var result = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);
        Assert.True(result.Success);

        var input = "福祉課 御中";
        var masked = fixture.Service.Mask(input, result.Rules);

        Assert.Equal(input, masked.MaskedText);
    }

    [Fact]
    public void DictionaryLoad_Succeeds_WhenOptionalKeysMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"maskedpi-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var rulePath = Path.Combine(tempDir, "rules.json");
        var dictPath = Path.Combine(tempDir, "dict.json");

        File.WriteAllText(rulePath, "{\"rules\":[]}");
        File.WriteAllText(dictPath, "{\"nameLabels\":[\"氏名\"]}");

        var service = new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine());
        var result = service.Load(rulePath, dictPath);

        Assert.True(result.Success);
    }

    private static TestFixture CreateFixture()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"maskedpi-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var rulePath = Path.Combine(tempDir, "rules.json");
        var dictionaryPath = Path.Combine(tempDir, "local_dictionary.json");

        var rules = new
        {
            rules = new[]
            {
                new
                {
                    name = "Fixed-ApplicationNumber",
                    enabled = true,
                    category = "LocalRule",
                    pattern = "申請番号[:：]?\\s*[0-9A-Za-z\\-]{3,}",
                    replacement = "[申請番号]",
                    priority = 12,
                    ignoreCase = false,
                    description = "fixed"
                },
                new
                {
                    name = "Fixed-GenericIdentifier",
                    enabled = true,
                    category = "LocalRule",
                    pattern = "[一-龥]{1,8}番号[:：]?\\s*[0-9A-Za-z\\-]{3,}",
                    replacement = "[識別子]",
                    priority = 90,
                    ignoreCase = false,
                    description = "generic"
                }
            }
        };

        var dictionary = new
        {
            nameLabels = new[] { "氏名", "申請者" },
            addressLabels = new[] { "住所", "現住所" },
            dateLabels = new[] { "生年月日", "申請日" },
            contactLabels = new[] { "電話番号", "メール" },
            localLabels = new[] { "申請番号", "相談管理番号" },
            idPrefixes = new[] { "SINSEI-", "CASE-" },
            idSuffixKeywords = new[] { "番号", "ID" },
            departmentNames = new[] { "福祉課", "税務課" },
            surnames = new[] { "佐藤" },
            givenNames = new[] { "太郎" }
        };

        File.WriteAllText(rulePath, JsonSerializer.Serialize(rules));
        File.WriteAllText(dictionaryPath, JsonSerializer.Serialize(dictionary));

        return new TestFixture(
            new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine()),
            rulePath,
            dictionaryPath);
    }

    private sealed record TestFixture(MaskingService Service, string RulePath, string DictionaryPath);
}

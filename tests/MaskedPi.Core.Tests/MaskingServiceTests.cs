using System.Text;
using System.Text.Json;
using MaskedPi.Core;

namespace MaskedPi.Core.Tests;

public sealed class MaskingServiceTests
{
    [Fact]
    public void RuleLoader_LoadsRulesJsonSuccessfully()
    {
        var repoRoot = FindRepoRoot();
        var rulePath = Path.Combine(repoRoot, "config", "rules.json");

        var result = new RuleLoader().Load(rulePath);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Rules);
    }

    [Fact]
    public void DictionaryProvider_LoadsLocalDictionarySuccessfully()
    {
        var repoRoot = FindRepoRoot();
        var dictPath = Path.Combine(repoRoot, "config", "local_dictionary.json");

        var result = new DictionaryProvider().Load(dictPath);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Dictionary.NameLabels);
    }

    [Fact]
    public void DictionaryLoad_Succeeds_WhenOptionalKeysMissing()
    {
        var tempDir = MakeTempDir();
        var rulePath = Path.Combine(tempDir, "rules.json");
        var dictPath = Path.Combine(tempDir, "dict.json");

        File.WriteAllText(rulePath, "{\"rules\":[]}");
        File.WriteAllText(dictPath, "{\"nameLabels\":[\"氏名\"]}");

        var service = CreateService();
        var result = service.Load(rulePath, dictPath);

        Assert.True(result.Success);
    }

    [Fact]
    public void RuleLoader_SkipsInvalidRule()
    {
        var tempDir = MakeTempDir();
        var rulePath = Path.Combine(tempDir, "rules.json");
        File.WriteAllText(rulePath, "{\"rules\":[{\"name\":\"bad\",\"pattern\":\"([\",\"enabled\":true}]}");

        var result = new RuleLoader().Load(rulePath);

        Assert.True(result.Success);
        Assert.Empty(result.Rules);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void PriorityAndOverlap_FirstWinnerOnly()
    {
        var service = CreateService();
        var rules = new List<RuleDefinition>
        {
            new() { Name = "Local-High", Enabled = true, Category = RuleCategory.LocalRule, Pattern = "申請番号[:：]?\\s*[0-9A-Za-z\\-]{3,}", Replacement = "[申請番号]", Priority = 10, Source = "fixed" },
            new() { Name = "Local-Low", Enabled = true, Category = RuleCategory.LocalRule, Pattern = "[一-龥]{1,8}番号[:：]?\\s*[0-9A-Za-z\\-]{3,}", Replacement = "[識別子]", Priority = 90, Source = "fixed" }
        };

        var masked = service.Mask("申請番号: SINSEI-1001", rules);

        Assert.Equal("[申請番号]", masked.MaskedText);
        Assert.Single(masked.Replacements);
        Assert.Equal("Local-High", masked.Replacements[0].RuleName);
    }

    [Fact]
    public void LocalRule_WinsOverPhoneRule_WhenOverlapExpected()
    {
        var rules = new List<RuleDefinition>
        {
            new() { Name = "Local-Hotline", Enabled = true, Category = RuleCategory.LocalRule, Pattern = "相談管理番号[:：]?\\s*090-1234-5678", Replacement = "[相談管理番号]", Priority = 10, Source = "fixed" },
            new() { Name = "Phone-General", Enabled = true, Category = RuleCategory.Phone, Pattern = "(?:0\\d{1,4}-?\\d{1,4}-?\\d{3,4})", Replacement = "[電話番号]", Priority = 40, Source = "fixed" }
        };

        var masked = CreateService().Mask("相談管理番号: 090-1234-5678", rules);

        Assert.Equal("[相談管理番号]", masked.MaskedText);
    }

    [Fact]
    public void Load_GeneratesNameLabelAndDepartmentRules()
    {
        var fixture = CreateFixture();
        var loaded = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);

        Assert.True(loaded.Success);
        Assert.Contains(loaded.Rules, r => r.Name.StartsWith("DynamicNameLabelRule::", StringComparison.Ordinal));
        Assert.Contains(loaded.Rules, r => r.Name.StartsWith("DynamicDepartmentPersonRule::", StringComparison.Ordinal));
        Assert.Contains(loaded.Rules, r => r.Name.StartsWith("DynamicAttributeLabelRule::", StringComparison.Ordinal));
        Assert.Contains(loaded.Rules, r => r.Name.StartsWith("DynamicAddressBuildingRule::", StringComparison.Ordinal));
        Assert.Contains(loaded.Rules, r => r.Name.StartsWith("DynamicNameContextRule::", StringComparison.Ordinal));
    }

    [Fact]
    public void DepartmentPersonRule_AvoidsCommonNonNameWords()
    {
        var fixture = CreateFixture();
        var loaded = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);

        var masked = fixture.Service.Mask("福祉課 御中", loaded.Rules, "welfare");
        Assert.Equal("福祉課 御中", masked.MaskedText);
    }

    [Fact]
    public void Mask_LongInput_DoesNotThrowAndReturnsOutput()
    {
        var fixture = CreateFixture();
        var loaded = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);

        var sb = new StringBuilder();
        for (var i = 0; i < 10000; i++)
        {
            sb.Append("申請番号: SINSEI-1001 ");
        }

        var masked = fixture.Service.Mask(sb.ToString(), loaded.Rules, "common");
        Assert.NotEmpty(masked.MaskedText);
    }

    [Fact]
    public void RuleTestCaseLoader_LoadsJsonCases()
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, "config", "test_cases.json");
        var result = new RuleTestCaseLoader().Load(path);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Cases);
    }

    [Fact]
    public void CommonProfile_UsesOnlyCommonRules()
    {
        var rules = new List<RuleDefinition>
        {
            new() { Name = "Common", Enabled = true, Category = RuleCategory.Other, Pattern = "AAA", Replacement = "[C]", Priority = 10, Profile = "common" },
            new() { Name = "Tax", Enabled = true, Category = RuleCategory.Other, Pattern = "AAA", Replacement = "[T]", Priority = 11, Profile = "tax" }
        };

        var masked = CreateService().Mask("AAA", rules, "common");
        Assert.Equal("[C]", masked.MaskedText);
    }

    [Fact]
    public void TaxProfile_UsesCommonAndTaxRules()
    {
        var rules = new List<RuleDefinition>
        {
            new() { Name = "Common", Enabled = true, Category = RuleCategory.Other, Pattern = "BBB", Replacement = "[C]", Priority = 10, Profile = "common" },
            new() { Name = "Tax", Enabled = true, Category = RuleCategory.Other, Pattern = "CCC", Replacement = "[T]", Priority = 10, Profile = "tax" }
        };

        var service = CreateService();
        var masked = service.Mask("BBB CCC", rules, "tax");
        Assert.Equal("[C] [T]", masked.MaskedText);
    }

    [Fact]
    public void WelfareProfile_DoesNotIncludeTaxRules()
    {
        var rules = new List<RuleDefinition>
        {
            new() { Name = "Tax", Enabled = true, Category = RuleCategory.Other, Pattern = "課税", Replacement = "[税務]", Priority = 10, Profile = "tax" }
        };

        var masked = CreateService().Mask("課税", rules, "welfare");
        Assert.Equal("課税", masked.MaskedText);
    }

    [Fact]
    public void ProfileByDictionaryKeys_AreLoaded()
    {
        var tempDir = MakeTempDir();
        var dictPath = Path.Combine(tempDir, "dict.json");
        File.WriteAllText(dictPath, "{\"departmentNamesByProfile\":{\"welfare\":[\"生活福祉課\"]}} ");

        var result = new DictionaryProvider().Load(dictPath);

        Assert.True(result.Success);
        Assert.True(result.Dictionary.DepartmentNamesByProfile.ContainsKey("welfare"));
    }

    [Fact]
    public void MissingRuleProfile_IsTreatedAsCommon()
    {
        var rules = new List<RuleDefinition>
        {
            new() { Name = "NoProfile", Enabled = true, Category = RuleCategory.Other, Pattern = "ZZZ", Replacement = "[X]", Priority = 10, Profile = "" }
        };

        var masked = CreateService().Mask("ZZZ", rules, "education");
        Assert.Equal("[X]", masked.MaskedText);
    }


    [Fact]
    public void ContextNameAndAttributeAndBuildingAddress_AreMasked()
    {
        var fixture = CreateFixture();
        var loaded = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);

        var input = "令和7年4月1日付けで、佐藤 太郎（生年月日：1985年3月15日、性別：男性）より提出。住所は沖縄県那覇市おもろまち2丁目3番4号 グリーンハイツ501号室です。";
        var masked = fixture.Service.Mask(input, loaded.Rules, "common");

        Assert.Contains("[日付]", masked.MaskedText);
        Assert.Contains("[氏名]", masked.MaskedText);
        Assert.Contains("[個人属性]", masked.MaskedText);
        Assert.Contains("[住所]", masked.MaskedText);
    }

    [Fact]
    public void SwitchingProfile_ChangesResult()
    {
        var rules = new List<RuleDefinition>
        {
            new() { Name = "WelfareRule", Enabled = true, Category = RuleCategory.LocalRule, Pattern = "ケース番号[:：]?\\s*[0-9]+", Replacement = "[ケース番号]", Priority = 10, Profile = "welfare" }
        };

        var service = CreateService();
        var input = "ケース番号: 123";

        var welfare = service.Mask(input, rules, "welfare");
        var tax = service.Mask(input, rules, "tax");

        Assert.NotEqual(welfare.MaskedText, tax.MaskedText);
    }

    private static TestFixture CreateFixture()
    {
        var tempDir = MakeTempDir();
        var rulePath = Path.Combine(tempDir, "rules.json");
        var dictionaryPath = Path.Combine(tempDir, "local_dictionary.json");

        var rules = new
        {
            rules = new[]
            {
                new { name = "Fixed-ApplicationNumber", enabled = true, category = "LocalRule", pattern = "申請番号[:：]?\\s*[0-9A-Za-z\\-]{3,}", replacement = "[申請番号]", priority = 12, ignoreCase = false, description = "fixed", profile = "common" },
                new { name = "Tax-Only", enabled = true, category = "LocalRule", pattern = "納税通知書番号[:：]?\\s*[0-9A-Za-z\\-]{3,}", replacement = "[納税通知書番号]", priority = 12, ignoreCase = false, description = "tax", profile = "tax" }
            }
        };

        var dictionary = new
        {
            nameLabels = new[] { "氏名", "申請者" },
            addressLabels = new[] { "住所", "現住所" },
            dateLabels = new[] { "生年月日", "申請日" },
            contactLabels = new[] { "電話番号", "メール" },
            attributeLabels = new[] { "生年月日", "性別", "続柄", "国籍", "電話番号", "携帯番号", "メールアドレス" },
            addressBuildingKeywords = new[] { "ハイツ", "マンション", "コーポ", "ビル", "アパート", "荘", "号室", "階" },
            localLabels = new[] { "申請番号", "相談管理番号" },
            localLabelsByProfile = new Dictionary<string, string[]> { ["tax"] = new[] { "納税通知書番号" }, ["welfare"] = new[] { "ケース番号" } },
            idPrefixes = new[] { "SINSEI-", "CASE-" },
            idSuffixKeywords = new[] { "番号", "ID" },
            departmentNames = new[] { "福祉課", "税務課" },
            departmentNamesByProfile = new Dictionary<string, string[]> { ["welfare"] = new[] { "生活福祉課" }, ["tax"] = new[] { "納税課" } },
            surnames = new[] { "佐藤" },
            givenNames = new[] { "太郎" }
        };

        File.WriteAllText(rulePath, JsonSerializer.Serialize(rules));
        File.WriteAllText(dictionaryPath, JsonSerializer.Serialize(dictionary));

        return new TestFixture(CreateService(), rulePath, dictionaryPath);
    }

    private static MaskingService CreateService()
        => new(new RuleLoader(), new DictionaryProvider(), new RuleEngine());

    private static string MakeTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), $"maskedpi-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "config")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record TestFixture(MaskingService Service, string RulePath, string DictionaryPath);
}

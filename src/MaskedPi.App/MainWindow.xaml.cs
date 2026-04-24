using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using MaskedPi.Core;

namespace MaskedPi.App;

public partial class MainWindow : Window
{
    private readonly MaskingService _maskingService;
    private List<RuleDefinition> _runtimeRules = new();
    private readonly ObservableCollection<SummaryRow> _summaryRows = new();
    private readonly ObservableCollection<TesterHitRow> _testerHitRows = new();

    private readonly string _rulePath;
    private readonly string _dictionaryPath;
    private readonly List<string> _profiles = MaskingService.SupportedProfiles.ToList();

    public MainWindow()
    {
        InitializeComponent();

        _maskingService = new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine());
        SummaryDataGrid.ItemsSource = _summaryRows;
        TesterHitsDataGrid.ItemsSource = _testerHitRows;

        ProfileComboBox.ItemsSource = _profiles;
        TesterProfileComboBox.ItemsSource = _profiles;
        ProfileComboBox.SelectedItem = MaskingService.CommonProfile;
        TesterProfileComboBox.SelectedItem = MaskingService.CommonProfile;

        var baseDir = AppContext.BaseDirectory;
        _rulePath = Path.Combine(baseDir, "config", "rules.json");
        _dictionaryPath = Path.Combine(baseDir, "config", "local_dictionary.json");

        LoadSettings();
        UpdateInputCount();
        SummaryHeaderTextBlock.Text = "置換件数: 0";
    }

    private void LoadSettings()
    {
        var result = _maskingService.Load(_rulePath, _dictionaryPath);
        if (!result.Success)
        {
            StatusTextBlock.Text = $"設定読み込み失敗: {result.ErrorMessage}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Firebrick;
            return;
        }

        _runtimeRules = result.Rules;
        if (result.Warnings.Count > 0)
        {
            StatusTextBlock.Text = $"設定読込完了（警告あり）: {string.Join(" / ", result.Warnings.Take(2))}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkGoldenrod;
        }
        else
        {
            StatusTextBlock.Text = $"設定読込完了: ルール {_runtimeRules.Count} 件";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkGreen;
        }
    }

    private void MaskButton_OnClick(object sender, RoutedEventArgs e)
    {
        var profile = ProfileComboBox.SelectedItem?.ToString() ?? MaskingService.CommonProfile;
        var result = _maskingService.Mask(InputTextBox.Text, _runtimeRules, profile);
        OutputTextBox.Text = result.MaskedText;

        SummaryHeaderTextBlock.Text = $"置換件数: {result.TotalReplacements}";
        _summaryRows.Clear();

        foreach (var category in Enum.GetValues<RuleCategory>())
        {
            result.CategoryCounts.TryGetValue(category, out var count);
            _summaryRows.Add(new SummaryRow(GetCategoryLabel(category), count));
        }
    }

    private void TesterRunButton_OnClick(object sender, RoutedEventArgs e)
    {
        var profile = TesterProfileComboBox.SelectedItem?.ToString() ?? MaskingService.CommonProfile;
        var result = _maskingService.Mask(TesterInputTextBox.Text, _runtimeRules, profile);
        TesterOutputTextBox.Text = result.MaskedText;

        _testerHitRows.Clear();
        foreach (var hit in result.Replacements.OrderBy(x => x.StartIndex))
        {
            _testerHitRows.Add(new TesterHitRow(
                hit.RuleName,
                GetCategoryLabel(hit.Category),
                hit.Priority,
                hit.OriginalText,
                hit.ReplacementText,
                hit.StartIndex,
                hit.Length,
                hit.Source));
        }

        TesterSummaryTextBlock.Text = $"ヒット件数: {result.TotalReplacements} / ルール数: {result.RuleHitCounts.Count} / profile: {profile}";
    }

    private void ReloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        LoadSettings();
    }

    private void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OutputTextBox.Text))
        {
            MessageBox.Show("出力結果が空です。", "MaskedPi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(OutputTextBox.Text);
        StatusTextBlock.Text = "マスキング結果をクリップボードにコピーしました。";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkGreen;
    }

    private void TesterCopyOutputButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TesterOutputTextBox.Text))
        {
            MessageBox.Show("テスト出力が空です。", "MaskedPi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(TesterOutputTextBox.Text);
        StatusTextBlock.Text = "テスター出力をコピーしました。";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkGreen;
    }

    private void TesterCopyDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_testerHitRows.Count == 0)
        {
            MessageBox.Show("ヒット詳細がありません。", "MaskedPi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("RuleName\tCategory\tPriority\tMatchedText\tReplacement\tStartIndex\tLength\tSource");
        foreach (var row in _testerHitRows)
        {
            sb.AppendLine($"{row.RuleName}\t{row.CategoryLabel}\t{row.Priority}\t{row.MatchedText}\t{row.Replacement}\t{row.StartIndex}\t{row.Length}\t{row.Source}");
        }

        Clipboard.SetText(sb.ToString());
        StatusTextBlock.Text = "ヒット詳細をクリップボードにコピーしました。";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkGreen;
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        InputTextBox.Clear();
        OutputTextBox.Clear();
        _summaryRows.Clear();
        SummaryHeaderTextBlock.Text = "置換件数: 0";
        UpdateInputCount();
    }

    private void InputTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateInputCount();
    }

    private void UpdateInputCount()
    {
        InputCountTextBlock.Text = $"文字数: {InputTextBox.Text.Length:N0}";
    }

    private static string GetCategoryLabel(RuleCategory category) => category switch
    {
        RuleCategory.LocalRule => "自治体独自ID / ローカルルール",
        RuleCategory.Phone => "電話番号",
        RuleCategory.Email => "メールアドレス",
        RuleCategory.PostalCode => "郵便番号",
        RuleCategory.Date => "日付 / 生年月日候補",
        RuleCategory.Address => "住所候補",
        RuleCategory.Name => "氏名候補",
        _ => "その他"
    };

    private sealed record SummaryRow(string CategoryLabel, int Count);

    private sealed record TesterHitRow(
        string RuleName,
        string CategoryLabel,
        int Priority,
        string MatchedText,
        string Replacement,
        int StartIndex,
        int Length,
        string Source);
}

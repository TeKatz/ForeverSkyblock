using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Keeps development-roadmap terminology out of the shipped editor UI without changing the
/// internal phase-oriented module names that still describe implementation history.
/// </summary>
public partial class MainWindow
{
    private static readonly Regex DevelopmentPhaseToken = new(
        @"\bPHASE\s+\d+(?:[A-Z])?(?:\.\d+)?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DevelopmentPhasePrefix = new(
        @"^\s*PHASE\s+\d+(?:[A-Z])?(?:\.\d+)?\s*[·\-–—:]\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DevelopmentPhaseSuffix = new(
        @"\s*[·\-–—:]\s*PHASE\s+\d+(?:[A-Z])?(?:\.\d+)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly DependencyPropertyDescriptor TextBlockTextDescriptor =
        DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock))!;

    private bool _developmentPhaseUiCleanupInitialized;

    public void EnableForeverDevelopmentPhaseUiCleanup()
    {
        if (_developmentPhaseUiCleanupInitialized)
            return;
        _developmentPhaseUiCleanupInitialized = true;

        // Controls created later by deferred editor modules are cleaned exactly when they load.
        // This deliberately avoids LayoutUpdated/render polling and never walks the full tree per frame.
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(DevelopmentPhaseUiCleanup_Loaded),
            true);

        // Only the two status surfaces known to receive late text updates need a change listener.
        // Animation counters and other frequently changing TextBlocks stay completely untouched.
        WatchDevelopmentPhaseStatus(StatusText);
        WatchDevelopmentPhaseStatus(_studioAnimationStatusText);
        Closed += (_, _) =>
        {
            UnwatchDevelopmentPhaseStatus(StatusText);
            UnwatchDevelopmentPhaseStatus(_studioAnimationStatusText);
        };
    }

    private void DevelopmentPhaseUiCleanup_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        if (element is TextBlock text)
            CleanDevelopmentPhaseTextBlock(text);

        if (element is HeaderedContentControl headered && headered.Header is string header)
            headered.Header = CleanDevelopmentPhaseControlLabel(header);

        if (element is ContentControl contentControl && contentControl.Content is string content)
            contentControl.Content = CleanDevelopmentPhaseControlLabel(content);

        if (element.ToolTip is string tooltip && DevelopmentPhaseToken.IsMatch(tooltip))
            element.ToolTip = null;
    }

    private static void WatchDevelopmentPhaseStatus(TextBlock? text)
    {
        if (text is not null)
            TextBlockTextDescriptor.AddValueChanged(text, DevelopmentPhaseStatusTextChanged);
    }

    private static void UnwatchDevelopmentPhaseStatus(TextBlock? text)
    {
        if (text is not null)
            TextBlockTextDescriptor.RemoveValueChanged(text, DevelopmentPhaseStatusTextChanged);
    }

    private static void DevelopmentPhaseStatusTextChanged(object? sender, EventArgs e)
    {
        if (sender is not TextBlock text || string.IsNullOrWhiteSpace(text.Text) || !DevelopmentPhaseToken.IsMatch(text.Text))
            return;

        var cleaned = Regex.Replace(
            text.Text,
            @"\bPHASE\s+\d+(?:[A-Z])?(?:\.\d+)?\s+base state\b",
            "Base state",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        cleaned = DevelopmentPhaseToken.Replace(cleaned, string.Empty);
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();
        cleaned = cleaned.TrimStart('·', '-', '–', '—', ':').TrimStart();

        if (cleaned.Length > 0 && char.IsLower(cleaned[0]))
            cleaned = char.ToUpperInvariant(cleaned[0]) + cleaned[1..];

        if (!string.Equals(cleaned, text.Text, StringComparison.Ordinal))
            text.Text = cleaned;
    }

    private static void CleanDevelopmentPhaseTextBlock(TextBlock text)
    {
        var value = text.Text?.Trim();
        if (string.IsNullOrEmpty(value))
            return;

        // A leading phase marker is pure development chrome (for example "PHASE 9" or
        // "PHASE 2 · PRODUCTION LIBRARY"), so remove the whole visual including a dedicated card/pill.
        if (DevelopmentPhasePrefix.IsMatch(value) ||
            Regex.IsMatch(value, @"^PHASE\s+\d+(?:[A-Z])?(?:\.\d+)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            CollapseDevelopmentPhaseText(text);
            return;
        }

        // Functional headings keep their useful name while dropping a trailing roadmap suffix.
        var withoutSuffix = DevelopmentPhaseSuffix.Replace(value, string.Empty).Trim();
        if (!string.Equals(withoutSuffix, value, StringComparison.Ordinal))
        {
            text.Text = withoutSuffix;
            return;
        }

        // Numbered phase prose is implementation-history/help copy rather than an editor control.
        if (DevelopmentPhaseToken.IsMatch(value) ||
            value.Contains("Machine Builder phase", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(value, @"\b(?:later|next|previous|current)\s+phase\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            CollapseDevelopmentPhaseText(text);
        }
    }

    private static void CollapseDevelopmentPhaseText(TextBlock text)
    {
        text.Visibility = Visibility.Collapsed;
        text.Margin = new Thickness(0);

        if (LogicalTreeHelper.GetParent(text) is Border border && ReferenceEquals(border.Child, text))
        {
            border.Visibility = Visibility.Collapsed;
            border.Margin = new Thickness(0);
        }
    }

    private static string CleanDevelopmentPhaseControlLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !DevelopmentPhaseToken.IsMatch(value))
            return value;

        var cleaned = DevelopmentPhaseSuffix.Replace(value, string.Empty).Trim();
        cleaned = DevelopmentPhasePrefix.Replace(cleaned, string.Empty).Trim();

        // A remaining phase reference means the whole string is explanatory development copy.
        return DevelopmentPhaseToken.IsMatch(cleaned) ? string.Empty : cleaned;
    }
}

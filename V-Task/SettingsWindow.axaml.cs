using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Linq;
using V_Task.Services;

namespace V_Task;

public partial class SettingsWindow : Window
{
    private readonly LocalizationService _localization = LocalizationService.Instance;
    private Action? _onLanguageChanged;

    public SettingsWindow()
    {
        InitializeComponent();
        InitializeLanguageComboBox();
        UpdateLocalizedStrings();
    }

    public void SetLanguageChangedCallback(Action callback)
    {
        _onLanguageChanged = callback;
    }

    private void InitializeLanguageComboBox()
    {
        // Select current language
        if (LanguageComboBox != null)
        {
            foreach (ComboBoxItem item in LanguageComboBox.Items.Cast<ComboBoxItem>())
            {
                if (item.Tag?.ToString() == _localization.CurrentLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void UpdateLocalizedStrings()
    {
        if (TitleText != null)
            TitleText.Text = _localization["Settings"];

        if (TabGeneral != null)
            TabGeneral.Content = $"🌐 {_localization["Language"]}";

        if (TabAbout != null)
            TabAbout.Content = $"ℹ️ {_localization["About"]}";

        if (HeaderLanguage != null)
            HeaderLanguage.Text = $"🌐 {_localization["Language"]}";

        if (LabelSelectLanguage != null)
            LabelSelectLanguage.Text = _localization["SelectLanguage"];

        if (HeaderAbout != null)
            HeaderAbout.Text = $"ℹ️ {_localization["AboutApp"]}";

        if (LabelVersion != null)
            LabelVersion.Text = _localization["Version"];

        if (LabelAuthor != null)
            LabelAuthor.Text = _localization["Author"];

        if (LabelDescription != null)
            LabelDescription.Text = _localization["Description"];

        if (TextDescription != null)
            TextDescription.Text = _localization["AppDescription"];

        if (LabelTechnologies != null)
            LabelTechnologies.Text = _localization["Technologies"];
    }

    private void SettingsTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        // Update button states
        if (TabGeneral != null) TabGeneral.Classes.Remove("active");
        if (TabAbout != null) TabAbout.Classes.Remove("active");

        button.Classes.Add("active");

        // Show corresponding panel
        if (PanelGeneral != null) PanelGeneral.IsVisible = false;
        if (PanelAbout != null) PanelAbout.IsVisible = false;

        if (button.Name == "TabGeneral" && PanelGeneral != null)
            PanelGeneral.IsVisible = true;
        else if (button.Name == "TabAbout" && PanelAbout != null)
            PanelAbout.IsVisible = true;
    }

    private void LanguageComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox?.SelectedItem is ComboBoxItem selectedItem)
        {
            var langCode = selectedItem.Tag?.ToString();
            if (!string.IsNullOrEmpty(langCode) && langCode != _localization.CurrentLanguage)
            {
                _localization.CurrentLanguage = langCode;
                UpdateLocalizedStrings();
                _onLanguageChanged?.Invoke();
            }
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

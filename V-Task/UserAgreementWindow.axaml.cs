using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using V_Task.Services;

namespace V_Task;

public partial class UserAgreementWindow : Window
{
    private readonly LocalizationService _localization = LocalizationService.Instance;

    public UserAgreementWindow()
    {
        InitializeComponent();
        UpdateLocalizedStrings();
    }

    private void UpdateLocalizedStrings()
    {
        Title = _localization["UserAgreementTitle"];

        if (TitleText != null)
            TitleText.Text = _localization["UserAgreementTitle"];

        if (HeaderText != null)
            HeaderText.Text = _localization["UserAgreement"];

        if (AgreementText != null)
            AgreementText.Text = _localization["UserAgreementText"];

        if (AcceptButton != null)
            AcceptButton.Content = _localization["Accept"];

        if (DeclineButton != null)
            DeclineButton.Content = _localization["Decline"];
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void DeclineButton_Click(object sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

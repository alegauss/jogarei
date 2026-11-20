using System.Windows.Controls;
using ChiakiNg.Settings;

namespace ChiakiNg.Views;

/// <summary>
/// PP16: the Config tab, and the last of the settings screen's nine.
///
/// The About button's text is the one thing on this tab that is built rather than written. The QML
/// builds it from Qt's application name with -ng appended; since PP861 it is built from the
/// product's name instead, because Qt's application name is the settings store's and stays Chiaki.
/// </summary>
public partial class ConfigSettingsView : UserControl
{
    public ConfigSettingsView()
    {
        InitializeComponent();
        AboutButton.Content = ConfigSettingsViewModel.AboutCaption(App.ProductName);
    }
}

using System.Windows;
using System.Windows.Controls;

namespace Transmiti.Views;

/// <summary>PP14: registration. A constructor, by the rule PP13 set.</summary>
public partial class RegistView : UserControl
{
    public RegistView() => InitializeComponent();

    /// <summary>PP878: somebody pressed Register; whoever opened the dialog answers it.</summary>
    public event Action? RegisterRequested;

    private void OnRegister(object sender, RoutedEventArgs e) => RegisterRequested?.Invoke();
}

using System.Windows;
using System.Windows.Controls;

namespace Transmiti.Views;

/// <summary>PP14: the manual host dialog. A constructor, by the rule PP13 set.</summary>
public partial class ManualHostView : UserControl
{
    public ManualHostView() => InitializeComponent();

    /// <summary>PP879: somebody pressed Add; whoever opened the dialog writes it.</summary>
    public event Action? AddRequested;

    private void OnAdd(object sender, RoutedEventArgs e) => AddRequested?.Invoke();
}

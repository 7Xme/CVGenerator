using System.Windows.Controls;
using CVGenerator.Models.SectionModels;

namespace CVGenerator.Views;

public partial class Step2ExperiencesView : UserControl
{
    public Step2ExperiencesView()
    {
        InitializeComponent();
    }

    private void CollapseButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if ((sender as System.Windows.FrameworkElement)?.DataContext is SectionBase section)
            section.IsCollapsed = !section.IsCollapsed;
    }
}

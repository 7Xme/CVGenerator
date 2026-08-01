using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CVGenerator.ViewModels;

namespace CVGenerator.Views;

public partial class Step3TemplateView : UserControl
{
    public Step3TemplateView()
    {
        InitializeComponent();
    }

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TemplateCardViewModel card }
            && DataContext is Step3TemplateViewModel vm)
            vm.SelectTemplateCommand.Execute(card);
    }
}

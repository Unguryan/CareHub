using System.Windows;
using System.Windows.Controls;
using CareHub.Desktop.ViewModels;

namespace CareHub.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void PassBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb && DataContext is MainViewModel vm)
            vm.LoginPassword = pb.Password;
    }
}

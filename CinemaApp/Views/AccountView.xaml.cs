using CinemaApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CinemaApp.Views;

public partial class AccountView : UserControl
{
    public AccountView()
    {
        InitializeComponent();
    }

    private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountViewModel vm)
            vm.LoginPassword = ((PasswordBox)sender).Password;
    }

    private void RegPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountViewModel vm)
            vm.RegisterPassword = ((PasswordBox)sender).Password;
    }
}

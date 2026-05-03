using CinemaApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CinemaApp.Views;

public partial class MyTicketsView : UserControl
{
    public MyTicketsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MyTicketsViewModel vm)
            vm.Load();
    }
}

using CinemaApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CinemaApp.Views;

public partial class MoviesView : UserControl
{
    public MoviesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MoviesViewModel vm)
            vm.Load();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaApp.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
}

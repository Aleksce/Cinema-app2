using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CinemaApp.ViewModels;

public partial class AccountViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty]
    private User? _currentUser;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _loginEmail = string.Empty;

    [ObservableProperty]
    private string _loginPassword = string.Empty;

    [ObservableProperty]
    private string _registerName = string.Empty;

    [ObservableProperty]
    private string _registerEmail = string.Empty;

    [ObservableProperty]
    private string _registerPassword = string.Empty;

    [ObservableProperty]
    private string _registerPhone = string.Empty;

    [ObservableProperty]
    private bool _showRegister;

    [ObservableProperty]
    private int _loyaltyProgress;

    public AccountViewModel(MainViewModel main)
    {
        _main = main;
    }

    public void Load()
    {
        IsLoggedIn = CurrentUser != null;
        if (CurrentUser != null)
        {
            LoyaltyProgress = Math.Min(CurrentUser.LoyaltyPoints % 1000 * 100 / 1000, 100);
            using var db = new CinemaDbContext();
            CurrentUser = db.Users.Find(CurrentUser.Id) ?? CurrentUser;
        }
    }

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            ErrorMessage = "Введите email и пароль";
            return;
        }
        using var db = new CinemaDbContext();
        var user = db.Users.FirstOrDefault(u => u.Email == LoginEmail);
        if (user == null || !DatabaseInitializer.CheckPassword(LoginPassword, user.PasswordHash))
        {
            ErrorMessage = "Неверный email или пароль";
            return;
        }
        CurrentUser = user;
        IsLoggedIn = true;
        ErrorMessage = string.Empty;
        LoyaltyProgress = Math.Min(user.LoyaltyPoints % 1000 * 100 / 1000, 100);
    }

    [RelayCommand]
    private void Register()
    {
        if (string.IsNullOrWhiteSpace(RegisterName) || string.IsNullOrWhiteSpace(RegisterEmail) || string.IsNullOrWhiteSpace(RegisterPassword))
        {
            ErrorMessage = "Заполните все обязательные поля";
            return;
        }
        using var db = new CinemaDbContext();
        if (db.Users.Any(u => u.Email == RegisterEmail))
        {
            ErrorMessage = "Пользователь с таким email уже существует";
            return;
        }
        var user = new User
        {
            FullName = RegisterName,
            Email = RegisterEmail,
            PasswordHash = DatabaseInitializer.CheckPassword("", "") ? "" : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(RegisterPassword + "_hashed")),
            Phone = RegisterPhone,
            LoyaltyPoints = 0,
            LoyaltyLevel = "Стандарт"
        };
        db.Users.Add(user);
        db.SaveChanges();
        CurrentUser = user;
        IsLoggedIn = true;
        ShowRegister = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void Logout()
    {
        CurrentUser = null;
        IsLoggedIn = false;
        LoginEmail = string.Empty;
        LoginPassword = string.Empty;
    }

    [RelayCommand]
    private void ToggleRegister() => ShowRegister = !ShowRegister;
}

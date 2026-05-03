using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace CinemaApp.ViewModels;

public partial class AccountViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    // Allowed email domains
    private static readonly string[] AllowedDomains = new[]
    {
        "gmail.com", "mail.ru", "bk.ru", "inbox.ru", "list.ru",
        "yandex.ru", "ya.ru", "rambler.ru", "outlook.com",
        "hotmail.com", "icloud.com", "me.com", "proton.me",
        "protonmail.com", "yahoo.com", "cinema.ru"
    };

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

    // Validation hints shown in real time
    [ObservableProperty]
    private string _phoneHint = string.Empty;

    [ObservableProperty]
    private string _emailHint = string.Empty;

    [ObservableProperty]
    private string _passwordHint = string.Empty;

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

    // Live validation callbacks
    partial void OnRegisterEmailChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { EmailHint = string.Empty; return; }
        var err = ValidateEmail(value);
        EmailHint = err ?? "✓";
    }

    partial void OnRegisterPhoneChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { PhoneHint = string.Empty; return; }
        var err = ValidatePhone(value);
        PhoneHint = err ?? "✓";
    }

    partial void OnRegisterPasswordChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { PasswordHint = string.Empty; return; }
        var err = ValidatePassword(value);
        PasswordHint = err ?? "✓";
    }

    // ── Validation helpers ──────────────────────────────────────────

    private static string? ValidateEmail(string email)
    {
        if (email.Length < 6) return "Слишком короткий email (мин. 6 символов)";
        if (email.Length > 100) return "Слишком длинный email (макс. 100 символов)";
        if (!email.Contains('@')) return "Email должен содержать @";

        var parts = email.Split('@');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            return "Некорректный формат email";

        var domain = parts[1].ToLowerInvariant();
        if (!AllowedDomains.Contains(domain))
            return $"Недопустимый домен. Используйте: gmail.com, mail.ru, yandex.ru, bk.ru и др.";

        return null;
    }

    private static string? ValidatePhone(string phone)
    {
        // Strip everything except digits and leading +
        var digits = Regex.Replace(phone, @"[^\d]", "");
        if (digits.Length < 10) return "Слишком короткий номер (мин. 10 цифр)";
        if (digits.Length > 11) return "Слишком длинный номер (макс. 11 цифр)";
        return null;
    }

    private static string? ValidatePassword(string password)
    {
        if (password.Length < 8) return "Пароль слишком короткий (мин. 8 символов)";
        if (password.Length > 64) return "Пароль слишком длинный (макс. 64 символа)";
        return null;
    }

    private static string? ValidateName(string name)
    {
        if (name.Trim().Length < 2) return "Введите имя (мин. 2 символа)";
        if (name.Length > 80) return "Имя слишком длинное (макс. 80 символов)";
        return null;
    }

    // ── Commands ────────────────────────────────────────────────────

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            ErrorMessage = "Введите email и пароль";
            return;
        }
        using var db = new CinemaDbContext();
        var user = db.Users.FirstOrDefault(u => u.Email == LoginEmail.Trim().ToLower());
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
        var nameErr = ValidateName(RegisterName);
        if (nameErr != null) { ErrorMessage = nameErr; return; }

        var emailErr = ValidateEmail(RegisterEmail);
        if (emailErr != null) { ErrorMessage = emailErr; return; }

        if (!string.IsNullOrWhiteSpace(RegisterPhone))
        {
            var phoneErr = ValidatePhone(RegisterPhone);
            if (phoneErr != null) { ErrorMessage = phoneErr; return; }
        }

        var passErr = ValidatePassword(RegisterPassword);
        if (passErr != null) { ErrorMessage = passErr; return; }

        using var db = new CinemaDbContext();
        var normalizedEmail = RegisterEmail.Trim().ToLower();
        if (db.Users.Any(u => u.Email == normalizedEmail))
        {
            ErrorMessage = "Пользователь с таким email уже существует";
            return;
        }

        var user = new User
        {
            FullName = RegisterName.Trim(),
            Email = normalizedEmail,
            PasswordHash = DatabaseInitializer.HashPassword(RegisterPassword),
            Phone = RegisterPhone.Trim(),
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
        ErrorMessage = string.Empty;
        PhoneHint = string.Empty;
        EmailHint = string.Empty;
        PasswordHint = string.Empty;
    }

    [RelayCommand]
    private void ToggleRegister()
    {
        ShowRegister = !ShowRegister;
        ErrorMessage = string.Empty;
    }
}

using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace CinemaApp.ViewModels;

// ── Country code registry ──────────────────────────────────────────────────
public class CountryCode
{
    public string Flag    { get; init; } = string.Empty;
    public string Name    { get; init; } = string.Empty;
    public string Code    { get; init; } = string.Empty; // e.g. "+7"
    public int    Digits  { get; init; }                 // expected LOCAL digits after code

    public string Display => $"{Flag} {Name} ({Code})";
}

public partial class AccountViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    // ── Allowed email domains ────────────────────────────────────────
    private static readonly string[] AllowedDomains =
    {
        "gmail.com", "mail.ru", "bk.ru", "inbox.ru", "list.ru",
        "yandex.ru", "ya.ru", "rambler.ru", "outlook.com",
        "hotmail.com", "icloud.com", "me.com", "proton.me",
        "protonmail.com", "yahoo.com", "cinema.ru"
    };

    // ── Country codes (flag · name · dial code · local-digit count) ──
    public static readonly IReadOnlyList<CountryCode> Countries = new List<CountryCode>
    {
        new() { Flag="🇷🇺", Name="Россия",          Code="+7",   Digits=10 },
        new() { Flag="🇰🇿", Name="Казахстан",        Code="+7",   Digits=10 },
        new() { Flag="🇺🇦", Name="Украина",           Code="+380", Digits=9  },
        new() { Flag="🇧🇾", Name="Беларусь",          Code="+375", Digits=9  },
        new() { Flag="🇺🇿", Name="Узбекистан",        Code="+998", Digits=9  },
        new() { Flag="🇦🇿", Name="Азербайджан",       Code="+994", Digits=9  },
        new() { Flag="🇦🇲", Name="Армения",           Code="+374", Digits=8  },
        new() { Flag="🇬🇪", Name="Грузия",            Code="+995", Digits=9  },
        new() { Flag="🇰🇬", Name="Кыргызстан",        Code="+996", Digits=9  },
        new() { Flag="🇹🇯", Name="Таджикистан",       Code="+992", Digits=9  },
        new() { Flag="🇹🇲", Name="Туркменистан",      Code="+993", Digits=8  },
        new() { Flag="🇩🇪", Name="Германия",          Code="+49",  Digits=10 },
        new() { Flag="🇫🇷", Name="Франция",           Code="+33",  Digits=9  },
        new() { Flag="🇬🇧", Name="Великобритания",    Code="+44",  Digits=10 },
        new() { Flag="🇺🇸", Name="США",               Code="+1",   Digits=10 },
        new() { Flag="🇨🇦", Name="Канада",            Code="+1",   Digits=10 },
        new() { Flag="🇨🇳", Name="Китай",             Code="+86",  Digits=11 },
        new() { Flag="🇯🇵", Name="Япония",            Code="+81",  Digits=10 },
        new() { Flag="🇰🇷", Name="Южная Корея",       Code="+82",  Digits=10 },
        new() { Flag="🇹🇷", Name="Турция",            Code="+90",  Digits=10 },
        new() { Flag="🇮🇳", Name="Индия",             Code="+91",  Digits=10 },
        new() { Flag="🇵🇱", Name="Польша",            Code="+48",  Digits=9  },
        new() { Flag="🇮🇹", Name="Италия",            Code="+39",  Digits=10 },
        new() { Flag="🇪🇸", Name="Испания",           Code="+34",  Digits=9  },
        new() { Flag="🇳🇱", Name="Нидерланды",        Code="+31",  Digits=9  },
    };

    // ── Observable properties ────────────────────────────────────────
    [ObservableProperty] private User?  _currentUser;
    [ObservableProperty] private bool   _isLoggedIn;
    [ObservableProperty] private string _loginEmail    = string.Empty;
    [ObservableProperty] private string _loginPassword = string.Empty;
    [ObservableProperty] private string _registerName  = string.Empty;
    [ObservableProperty] private string _registerEmail = string.Empty;

    /// <summary>Local digits only — no country code prefix.</summary>
    [ObservableProperty] private string _registerPhone = string.Empty;

    [ObservableProperty] private string _registerPassword = string.Empty;
    [ObservableProperty] private bool   _showRegister;
    [ObservableProperty] private int    _loyaltyProgress;

    // ── Selected country for phone picker ───────────────────────────
    [ObservableProperty] private CountryCode _selectedCountry = Countries[0]; // Россия +7

    // ── Live validation hints ────────────────────────────────────────
    [ObservableProperty] private string _phoneHint    = string.Empty;
    [ObservableProperty] private string _emailHint    = string.Empty;
    [ObservableProperty] private string _passwordHint = string.Empty;

    public AccountViewModel(MainViewModel main) => _main = main;

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

    // ── Live validation callbacks ────────────────────────────────────

    partial void OnRegisterEmailChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { EmailHint = string.Empty; return; }
        EmailHint = ValidateEmail(value) ?? "✓";
    }

    partial void OnRegisterPhoneChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { PhoneHint = string.Empty; return; }
        PhoneHint = ValidateLocalPhone(value, SelectedCountry) ?? "✓";
    }

    partial void OnSelectedCountryChanged(CountryCode value)
    {
        // Re-validate the phone when the country changes
        if (string.IsNullOrWhiteSpace(RegisterPhone)) { PhoneHint = string.Empty; return; }
        PhoneHint = ValidateLocalPhone(RegisterPhone, value) ?? "✓";
    }

    partial void OnRegisterPasswordChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { PasswordHint = string.Empty; return; }
        PasswordHint = ValidatePassword(value) ?? "✓";
    }

    // ── Validation helpers ───────────────────────────────────────────

    private static string? ValidateEmail(string email)
    {
        if (email.Length > 100) return "Email слишком длинный (макс. 100 символов)";
        if (email.Length < 6)   return "Email слишком короткий (мин. 6 символов)";
        if (!email.Contains('@')) return "Email должен содержать символ @";

        var parts = email.Split('@');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            return "Некорректный формат email";

        if (parts[0].Length > 64) return "Часть до @ слишком длинная";

        var domain = parts[1].ToLowerInvariant();
        if (!AllowedDomains.Contains(domain))
            return "Недопустимый домен. Используйте gmail.com, mail.ru, yandex.ru и др.";

        return null;
    }

    /// <summary>Validates only the LOCAL part (without country code).</summary>
    private static string? ValidateLocalPhone(string localDigits, CountryCode country)
    {
        // Strip any non-digit characters user might type
        var digits = Regex.Replace(localDigits, @"\D", "");
        if (digits.Length == 0) return "Введите номер телефона";
        if (digits.Length < country.Digits)
            return $"Для {country.Name} нужно {country.Digits} цифр после {country.Code} (введено {digits.Length})";
        if (digits.Length > country.Digits)
            return $"Для {country.Name} нужно ровно {country.Digits} цифр (введено {digits.Length})";
        return null;
    }

    /// <summary>
    /// Password rules:
    ///  • 8–24 characters
    ///  • Only Latin letters (a-z A-Z) and digits (0-9)
    ///  • Must NOT start with a digit
    ///  • No spaces, no special characters, no Cyrillic
    /// </summary>
    private static string? ValidatePassword(string password)
    {
        if (password.Length < 8)  return "Пароль слишком короткий — минимум 8 символов";
        if (password.Length > 24) return "Пароль слишком длинный — максимум 24 символа";

        // Only Latin letters and digits allowed
        if (!Regex.IsMatch(password, @"^[A-Za-z0-9]+$"))
        {
            if (Regex.IsMatch(password, @"[А-Яа-яЁё]"))
                return "Пароль не должен содержать русские буквы";
            return "Пароль может содержать только латинские буквы (A–Z) и цифры (0–9)";
        }

        // Must not start with a digit
        if (char.IsDigit(password[0]))
            return "Пароль не должен начинаться с цифры";

        // Must contain at least one letter
        if (!Regex.IsMatch(password, @"[A-Za-z]"))
            return "Пароль должен содержать хотя бы одну букву";

        return null;
    }

    private static string? ValidateName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length < 2)  return "Введите имя (минимум 2 символа)";
        if (trimmed.Length > 80) return "Имя слишком длинное (максимум 80 символов)";
        if (!Regex.IsMatch(trimmed, @"^[\p{L}\s\-']+$"))
            return "Имя содержит недопустимые символы";
        return null;
    }

    // ── Commands ─────────────────────────────────────────────────────

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            ErrorMessage = "Введите email и пароль";
            return;
        }
        if (LoginEmail.Length > 200 || LoginPassword.Length > 200)
        {
            ErrorMessage = "Слишком длинные данные";
            return;
        }
        using var db = new CinemaDbContext();
        var user = db.Users.FirstOrDefault(u => u.Email == LoginEmail.Trim().ToLower());
        if (user == null || !DatabaseInitializer.CheckPassword(LoginPassword, user.PasswordHash))
        {
            ErrorMessage = "Неверный email или пароль";
            return;
        }
        CurrentUser   = user;
        IsLoggedIn    = true;
        ErrorMessage  = string.Empty;
        LoyaltyProgress = Math.Min(user.LoyaltyPoints % 1000 * 100 / 1000, 100);
    }

    [RelayCommand]
    private void Register()
    {
        var nameErr = ValidateName(RegisterName);
        if (nameErr != null) { ErrorMessage = nameErr; return; }

        var emailErr = ValidateEmail(RegisterEmail);
        if (emailErr != null) { ErrorMessage = emailErr; return; }

        // Phone is optional, but if provided it must be valid
        string fullPhone = string.Empty;
        if (!string.IsNullOrWhiteSpace(RegisterPhone))
        {
            var phoneErr = ValidateLocalPhone(RegisterPhone, SelectedCountry);
            if (phoneErr != null) { ErrorMessage = phoneErr; return; }
            // Combine country code + local digits
            var localDigits = Regex.Replace(RegisterPhone, @"\D", "");
            fullPhone = SelectedCountry.Code + localDigits;
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
            FullName      = RegisterName.Trim(),
            Email         = normalizedEmail,
            PasswordHash  = DatabaseInitializer.HashPassword(RegisterPassword),
            Phone         = fullPhone,
            LoyaltyPoints = 0,
            LoyaltyLevel  = "Стандарт"
        };
        db.Users.Add(user);
        db.SaveChanges();

        CurrentUser   = user;
        IsLoggedIn    = true;
        ShowRegister  = false;
        ErrorMessage  = string.Empty;
    }

    [RelayCommand]
    private void Logout()
    {
        CurrentUser   = null;
        IsLoggedIn    = false;
        LoginEmail    = string.Empty;
        LoginPassword = string.Empty;
        ErrorMessage  = string.Empty;
        PhoneHint     = string.Empty;
        EmailHint     = string.Empty;
        PasswordHint  = string.Empty;
        RegisterName  = string.Empty;
        RegisterEmail = string.Empty;
        RegisterPhone = string.Empty;
        RegisterPassword = string.Empty;
    }

    [RelayCommand]
    private void ToggleRegister()
    {
        ShowRegister     = !ShowRegister;
        ErrorMessage     = string.Empty;
        PhoneHint        = string.Empty;
        EmailHint        = string.Empty;
        PasswordHint     = string.Empty;
        RegisterName     = string.Empty;
        RegisterEmail    = string.Empty;
        RegisterPhone    = string.Empty;
        RegisterPassword = string.Empty;
    }
}

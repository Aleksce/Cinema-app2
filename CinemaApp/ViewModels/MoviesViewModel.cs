using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CinemaApp.ViewModels;

public partial class MoviesViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty] private ObservableCollection<Movie> _movies         = new();
    [ObservableProperty] private ObservableCollection<Movie> _filteredMovies = new();
    [ObservableProperty] private string _searchText    = string.Empty;
    [ObservableProperty] private string _selectedGenre = "Все";
    [ObservableProperty] private bool   _isEmpty;
    [ObservableProperty] private string _syncStatus = string.Empty;

    public List<string> Genres { get; } = new()
    {
        "Все", "Боевик", "Драма", "Фантастика", "Ужасы",
        "Мультфильм", "Приключения", "Комедия"
    };

    public MoviesViewModel(MainViewModel main) => _main = main;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await Task.Run(() =>
            {
                using var db = new CinemaDbContext();
                return db.Movies
                    .Where(m => m.IsActive)
                    .OrderByDescending(m => m.ImdbRating)
                    .ToList();
            });

            Movies = new ObservableCollection<Movie>(list);
            ApplyFilter();
        }
        finally { IsBusy = false; }
    }

    public void Load() => _ = LoadAsync();

    partial void OnSearchTextChanged(string value)    => ApplyFilter();
    partial void OnSelectedGenreChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        // Guard: very long search string — cap at 100 chars to avoid performance issues
        var query = SearchText.Length > 100 ? SearchText[..100] : SearchText;

        var filtered = Movies.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(m =>
                m.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(m.OriginalTitle) &&
                 m.OriginalTitle.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(m.Director) &&
                 m.Director.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (SelectedGenre != "Все")
            filtered = filtered.Where(m => m.Genre.Contains(SelectedGenre));

        FilteredMovies = new ObservableCollection<Movie>(filtered);
        IsEmpty        = !FilteredMovies.Any();
    }

    [RelayCommand]
    private void SelectGenre(string genre) => SelectedGenre = genre;

    [RelayCommand]
    private void SelectMovie(Movie movie) => _main.NavigateTo(AppPage.MovieDetail, movie);
}

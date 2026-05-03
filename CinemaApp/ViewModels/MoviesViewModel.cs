using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CinemaApp.ViewModels;

public partial class MoviesViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty]
    private ObservableCollection<Movie> _movies = new();

    [ObservableProperty]
    private ObservableCollection<Movie> _filteredMovies = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedGenre = "Все";

    [ObservableProperty]
    private bool _isEmpty;

    public List<string> Genres { get; } = new()
    {
        "Все", "Боевик", "Драма", "Фантастика", "Ужасы", "Мультфильм", "Приключения", "Комедия"
    };

    public MoviesViewModel(MainViewModel main)
    {
        _main = main;
    }

    public void Load()
    {
        IsBusy = true;
        using var db = new CinemaDbContext();
        var list = db.Movies.Where(m => m.IsActive).OrderByDescending(m => m.ImdbRating).ToList();
        Movies = new ObservableCollection<Movie>(list);
        ApplyFilter();
        IsBusy = false;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedGenreChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = Movies.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(m => m.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        if (SelectedGenre != "Все")
            filtered = filtered.Where(m => m.Genre.Contains(SelectedGenre));
        FilteredMovies = new ObservableCollection<Movie>(filtered);
        IsEmpty = !FilteredMovies.Any();
    }

    [RelayCommand]
    private void SelectGenre(string genre) => SelectedGenre = genre;

    [RelayCommand]
    private void SelectMovie(Movie movie) => _main.NavigateTo(AppPage.MovieDetail, movie);
}

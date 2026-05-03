using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CinemaApp.ViewModels;

public enum AppPage
{
    Movies,
    MovieDetail,
    SeatPicker,
    MyTickets,
    Schedule,
    Promotions,
    Account
}

public partial class MainViewModel : BaseViewModel
{
    [ObservableProperty]
    private AppPage _currentPage = AppPage.Movies;

    [ObservableProperty]
    private BaseViewModel? _currentViewModel;

    public MoviesViewModel MoviesVm { get; }
    public MovieDetailViewModel MovieDetailVm { get; }
    public SeatPickerViewModel SeatPickerVm { get; }
    public MyTicketsViewModel MyTicketsVm { get; }
    public ScheduleViewModel ScheduleVm { get; }
    public PromotionsViewModel PromotionsVm { get; }
    public AccountViewModel AccountVm { get; }

    public MainViewModel()
    {
        AccountVm = new AccountViewModel(this);
        MoviesVm = new MoviesViewModel(this);
        MovieDetailVm = new MovieDetailViewModel(this);
        SeatPickerVm = new SeatPickerViewModel(this);
        MyTicketsVm = new MyTicketsViewModel(this);
        ScheduleVm = new ScheduleViewModel(this);
        PromotionsVm = new PromotionsViewModel();

        NavigateTo(AppPage.Movies);
    }

    public void NavigateTo(AppPage page, object? parameter = null)
    {
        CurrentPage = page;
        switch (page)
        {
            case AppPage.Movies:
                MoviesVm.Load();
                CurrentViewModel = MoviesVm;
                break;
            case AppPage.MovieDetail:
                if (parameter is Models.Movie movie)
                    MovieDetailVm.LoadMovie(movie);
                CurrentViewModel = MovieDetailVm;
                break;
            case AppPage.SeatPicker:
                if (parameter is Models.Session session)
                    SeatPickerVm.LoadSession(session);
                CurrentViewModel = SeatPickerVm;
                break;
            case AppPage.MyTickets:
                MyTicketsVm.Load();
                CurrentViewModel = MyTicketsVm;
                break;
            case AppPage.Schedule:
                ScheduleVm.Load();
                CurrentViewModel = ScheduleVm;
                break;
            case AppPage.Promotions:
                CurrentViewModel = PromotionsVm;
                break;
            case AppPage.Account:
                AccountVm.Load();
                CurrentViewModel = AccountVm;
                break;
        }
    }

    [RelayCommand]
    private void GoToMovies() => NavigateTo(AppPage.Movies);

    [RelayCommand]
    private void GoToSchedule() => NavigateTo(AppPage.Schedule);

    [RelayCommand]
    private void GoToTickets() => NavigateTo(AppPage.MyTickets);

    [RelayCommand]
    private void GoToPromotions() => NavigateTo(AppPage.Promotions);

    [RelayCommand]
    private void GoToAccount() => NavigateTo(AppPage.Account);
}

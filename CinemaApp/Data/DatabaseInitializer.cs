using CinemaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApp.Data;

public static class DatabaseInitializer
{
    public static void Initialize(CinemaDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Movies.Any()) return;

        var hall1 = new Hall { Name = "Зал 1 — Dolby Atmos", TotalRows = 12, SeatsPerRow = 14 };
        var hall2 = new Hall { Name = "Зал 2 — IMAX", TotalRows = 10, SeatsPerRow = 12 };
        var hall3 = new Hall { Name = "Зал 3 — VIP", TotalRows = 8, SeatsPerRow = 10 };
        context.Halls.AddRange(hall1, hall2, hall3);

        var movies = new[]
        {
            new Movie
            {
                Title = "Дюна: Часть вторая", OriginalTitle = "Dune: Part Two",
                Description = "Пол Атрейдес объединяется с Чани и фрименами, встав на путь мести против заговорщиков, уничтоживших его семью.",
                Genre = "Фантастика", Director = "Дени Вильнёв",
                Cast = "Тимоти Шаламе, Зендая, Ребекка Фергюсон",
                DurationMinutes = 166, Year = 2024, AgeRating = "12+",
                ImdbRating = 8.5, ReleaseDate = new DateTime(2024, 3, 1),
                PosterUrl = "https://image.tmdb.org/t/p/w500/cdqLnri3NEGcmfnqwk2TSIYtddg.jpg"
            },
            new Movie
            {
                Title = "Оппенгеймер", OriginalTitle = "Oppenheimer",
                Description = "История Дж. Роберта Оппенгеймера и его роли в разработке атомной бомбы во время Второй мировой войны.",
                Genre = "Драма, Биография", Director = "Кристофер Нолан",
                Cast = "Киллиан Мёрфи, Эмили Блант, Мэтт Дэймон",
                DurationMinutes = 180, Year = 2023, AgeRating = "18+",
                ImdbRating = 8.9, ReleaseDate = new DateTime(2023, 7, 21),
                PosterUrl = "https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg"
            },
            new Movie
            {
                Title = "Гладиатор 2", OriginalTitle = "Gladiator II",
                Description = "Продолжение эпической саги о Риме. Новый воин встаёт на путь мести в Колизее.",
                Genre = "Боевик, Приключения", Director = "Ридли Скотт",
                Cast = "Пол Мескал, Дензел Вашингтон, Педро Паскаль",
                DurationMinutes = 148, Year = 2024, AgeRating = "16+",
                ImdbRating = 7.4, ReleaseDate = new DateTime(2024, 11, 22),
                PosterUrl = "https://image.tmdb.org/t/p/w500/2cxhvwyEwRlysAmRH4iodkvo0z5.jpg"
            },
            new Movie
            {
                Title = "Головоломка 2", OriginalTitle = "Inside Out 2",
                Description = "Райли взрослеет, и в штабе управления эмоциями появляются новые чувства: Тревога, Зависть и другие.",
                Genre = "Мультфильм, Семейный", Director = "Келси Манн",
                Cast = "Эми Поэлер, Майя Хоук, Тони Хэйл",
                DurationMinutes = 100, Year = 2024, AgeRating = "6+",
                ImdbRating = 7.9, ReleaseDate = new DateTime(2024, 6, 14),
                PosterUrl = "https://image.tmdb.org/t/p/w500/vpnVM9B6NMmQpWeZvzLvDESb2QY.jpg"
            },
            new Movie
            {
                Title = "Чужой: Ромул", OriginalTitle = "Alien: Romulus",
                Description = "Молодые космические колонисты оказываются лицом к лицу с самой страшной формой жизни во вселенной.",
                Genre = "Ужасы, Фантастика", Director = "Феде Альварес",
                Cast = "Кейли Спэни, Дэвид Джонссон, Арчи Рено",
                DurationMinutes = 119, Year = 2024, AgeRating = "18+",
                ImdbRating = 7.3, ReleaseDate = new DateTime(2024, 8, 16),
                PosterUrl = "https://image.tmdb.org/t/p/w500/b33nnKl1GSFbao4l3fZDDqsMx0F.jpg"
            },
            new Movie
            {
                Title = "Веном: Последний танец", OriginalTitle = "Venom: The Last Dance",
                Description = "Эдди Брок и Веном бегут от обоих миров, стремясь принять окончательное решение.",
                Genre = "Боевик, Фантастика", Director = "Келли Марсель",
                Cast = "Том Харди, Кьяра Аулетта, Джуно Темпл",
                DurationMinutes = 109, Year = 2024, AgeRating = "12+",
                ImdbRating = 6.1, ReleaseDate = new DateTime(2024, 10, 25),
                PosterUrl = "https://image.tmdb.org/t/p/w500/aosm8NMQ3UyoBVpSxyimorCQykC.jpg"
            },
        };
        context.Movies.AddRange(movies);
        context.SaveChanges();

        var now = DateTime.Today;
        var sessions = new List<Session>();
        var rng = new Random(42);
        foreach (var movie in movies)
        {
            for (int d = 0; d < 5; d++)
            {
                var times = new[] { 10, 13, 16, 19, 22 };
                foreach (var t in times.Take(rng.Next(2, 5)))
                {
                    var hall = new[] { hall1, hall2, hall3 }[rng.Next(3)];
                    sessions.Add(new Session
                    {
                        Movie = movie,
                        Hall = hall,
                        StartTime = now.AddDays(d).AddHours(t),
                        EndTime = now.AddDays(d).AddHours(t).AddMinutes(movie.DurationMinutes + 20),
                        Format = rng.Next(2) == 0 ? "2D" : "3D",
                        BasePrice = 300m + rng.Next(0, 4) * 50m
                    });
                }
            }
        }
        context.Sessions.AddRange(sessions);
        context.SaveChanges();

        var rows = "ABCDEFGHIJKLM";
        foreach (var session in sessions)
        {
            var seats = new List<Seat>();
            for (int r = 0; r < session.Hall.TotalRows; r++)
            {
                for (int n = 1; n <= session.Hall.SeatsPerRow; n++)
                {
                    var type = (r >= session.Hall.TotalRows - 2) ? SeatType.VIP : SeatType.Standard;
                    seats.Add(new Seat
                    {
                        Hall = session.Hall,
                        Session = session,
                        Row = rows[r].ToString(),
                        Number = n,
                        Status = rng.Next(5) == 0 ? SeatStatus.Occupied : SeatStatus.Available,
                        Type = type,
                        PriceModifier = type == SeatType.VIP ? 1.5m : 1.0m
                    });
                }
            }
            context.Seats.AddRange(seats);
        }

        var adminUser = new User
        {
            FullName = "Алексей Морозов",
            Email = "user@cinema.ru",
            PasswordHash = BCryptHash("password123"),
            Phone = "+7 (900) 123-45-67",
            LoyaltyPoints = 1240,
            LoyaltyLevel = "Синема Клуб",
            IsAdmin = false
        };
        context.Users.Add(adminUser);
        context.SaveChanges();
    }

    private static string BCryptHash(string password)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "_hashed"));
    }

    public static bool CheckPassword(string password, string hash)
    {
        return hash == BCryptHash(password);
    }
}

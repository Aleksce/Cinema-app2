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
        var hall4 = new Hall { Name = "Зал 4 — 4DX", TotalRows = 9, SeatsPerRow = 11 };
        context.Halls.AddRange(hall1, hall2, hall3, hall4);

        var movies = new[]
        {
            new Movie
            {
                Title = "Дюна: Часть вторая", OriginalTitle = "Dune: Part Two",
                Description = "Пол Атрейдес объединяется с Чани и фрименами, встав на путь мести против заговорщиков, уничтоживших его семью. Не желая повторить судьбу своего отца, он решает взять в свои руки самое дорогое из того, что у него есть — будущее.",
                Genre = "Фантастика", Director = "Дени Вильнёв",
                Cast = "Тимоти Шаламе, Зендая, Ребекка Фергюсон, Остин Батлер",
                DurationMinutes = 166, Year = 2024, AgeRating = "12+",
                ImdbRating = 8.5, ReleaseDate = new DateTime(2024, 3, 1),
                PosterUrl = "https://image.tmdb.org/t/p/w500/cdqLnri3NEGcmfnqwk2TSIYtddg.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=Way9Dexny3w"
            },
            new Movie
            {
                Title = "Оппенгеймер", OriginalTitle = "Oppenheimer",
                Description = "Захватывающий байопик о жизни Дж. Роберта Оппенгеймера — физика, возглавившего Манхэттенский проект по созданию первой в мире атомной бомбы. История о гении, власти и моральной ответственности учёного перед человечеством.",
                Genre = "Драма, Биография", Director = "Кристофер Нолан",
                Cast = "Киллиан Мёрфи, Эмили Блант, Мэтт Дэймон, Роберт Дауни мл.",
                DurationMinutes = 180, Year = 2023, AgeRating = "18+",
                ImdbRating = 8.9, ReleaseDate = new DateTime(2023, 7, 21),
                PosterUrl = "https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=uYPbbksJxIg"
            },
            new Movie
            {
                Title = "Гладиатор 2", OriginalTitle = "Gladiator II",
                Description = "Продолжение эпической саги о Риме. Спустя десятилетия после событий оригинального фильма, новый воин встаёт на путь мести в Колизее, чтобы вернуть себе то, что у него отняли.",
                Genre = "Боевик, Приключения", Director = "Ридли Скотт",
                Cast = "Пол Мескал, Дензел Вашингтон, Педро Паскаль, Конни Нильсен",
                DurationMinutes = 148, Year = 2024, AgeRating = "16+",
                ImdbRating = 7.4, ReleaseDate = new DateTime(2024, 11, 22),
                PosterUrl = "https://image.tmdb.org/t/p/w500/2cxhvwyEwRlysAmRH4iodkvo0z5.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=hWa6mvwSgDo"
            },
            new Movie
            {
                Title = "Головоломка 2", OriginalTitle = "Inside Out 2",
                Description = "Райли взрослеет, и в штабе управления эмоциями происходит что-то непредвиденное: появляются новые, более сложные чувства — Тревога, Зависть, Скука и Ностальгия. Старым эмоциям приходится уступить место.",
                Genre = "Мультфильм, Семейный", Director = "Келси Манн",
                Cast = "Эми Поэлер, Майя Хоук, Тони Хэйл, Лиза Ламбер",
                DurationMinutes = 100, Year = 2024, AgeRating = "6+",
                ImdbRating = 7.9, ReleaseDate = new DateTime(2024, 6, 14),
                PosterUrl = "https://image.tmdb.org/t/p/w500/vpnVM9B6NMmQpWeZvzLvDESb2QY.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=LEjhY15eCx0"
            },
            new Movie
            {
                Title = "Чужой: Ромул", OriginalTitle = "Alien: Romulus",
                Description = "Молодые космические колонисты отправляются на исследование заброшенной космической станции и оказываются лицом к лицу с самой страшной формой жизни во вселенной — ксеноморфами.",
                Genre = "Ужасы, Фантастика", Director = "Феде Альварес",
                Cast = "Кейли Спэни, Дэвид Джонссон, Арчи Рено, Изабела Мерсед",
                DurationMinutes = 119, Year = 2024, AgeRating = "18+",
                ImdbRating = 7.3, ReleaseDate = new DateTime(2024, 8, 16),
                PosterUrl = "https://image.tmdb.org/t/p/w500/b33nnKl1GSFbao4l3fZDDqsMx0F.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=x2FxKcOHGjk"
            },
            new Movie
            {
                Title = "Веном: Последний танец", OriginalTitle = "Venom: The Last Dance",
                Description = "Эдди Брок и Веном в бегах. Преследуемые со всех сторон, они должны принять окончательное решение и пойти на жертвы, чтобы спасти оба мира.",
                Genre = "Боевик, Фантастика", Director = "Келли Марсель",
                Cast = "Том Харди, Кьяра Аулетта, Джуно Темпл, Рис Иванс",
                DurationMinutes = 109, Year = 2024, AgeRating = "12+",
                ImdbRating = 6.1, ReleaseDate = new DateTime(2024, 10, 25),
                PosterUrl = "https://image.tmdb.org/t/p/w500/aosm8NMQ3UyoBVpSxyimorCQykC.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=j4eoNh6xiFI"
            },
            new Movie
            {
                Title = "Дэдпул и Россомаха", OriginalTitle = "Deadpool & Wolverine",
                Description = "Уэйд Уилсон облачается в наряд Дэдпула и встречается с Росомахой. Вместе им предстоит спасти мир — или хотя бы попытаться, пока хватит терпения.",
                Genre = "Боевик, Комедия", Director = "Шон Леви",
                Cast = "Райан Рейнольдс, Хью Джекман, Эмма Корин, Морена Баккарин",
                DurationMinutes = 127, Year = 2024, AgeRating = "18+",
                ImdbRating = 7.8, ReleaseDate = new DateTime(2024, 7, 26),
                PosterUrl = "https://image.tmdb.org/t/p/w500/8cdWjvZQUExUUTzyp4t6EDMubfO.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=73_1biulkYk"
            },
            new Movie
            {
                Title = "Трансформеры: Восстание зверей", OriginalTitle = "Transformers: Rise of the Beasts",
                Description = "1994 год. Новый раскол среди трансформеров угрожает уничтожением всей Земли. Оптимус Прайм и Бамблби объединяются с новыми союзниками — Максималами, чтобы противостоять Юникрону.",
                Genre = "Боевик, Фантастика", Director = "Стивен Кейпл мл.",
                Cast = "Энтони Рамос, Доминик Фишбэк, Питер Кульен",
                DurationMinutes = 127, Year = 2023, AgeRating = "12+",
                ImdbRating = 6.0, ReleaseDate = new DateTime(2023, 6, 9),
                PosterUrl = "https://image.tmdb.org/t/p/w500/gPbM0MK8CP8A174rmUwGsADNYKD.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=dWZpQi2TdzQ"
            },
            new Movie
            {
                Title = "Мастер и Маргарита", OriginalTitle = "The Master and Margarita",
                Description = "Экранизация легендарного романа Булгакова. Москва, 1930-е годы. Загадочный иностранец Воланд появляется в городе и устраивает настоящий хаос. Одновременно разворачивается история любви Мастера и Маргариты.",
                Genre = "Фэнтези, Драма", Director = "Михаил Локшин",
                Cast = "Евгений Цыганов, Юлия Снигирь, Аугуст Диль, Клас Банг",
                DurationMinutes = 157, Year = 2024, AgeRating = "18+",
                ImdbRating = 7.7, ReleaseDate = new DateTime(2024, 1, 25),
                PosterUrl = "https://image.tmdb.org/t/p/w500/eBIoTHKMRaY6UFwtHlXZ5p5BFAD.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=D4bpXSjAU8A"
            },
            new Movie
            {
                Title = "Бетмен", OriginalTitle = "The Batman",
                Description = "В течение двух лет Брюс Уэйн надевает тёмный плащ и действует в тени. Но когда серийный убийца начинает уничтожать городскую элиту, оставляя зашифрованные послания, Бэтмен вынужден выйти на свет.",
                Genre = "Боевик, Триллер", Director = "Мэтт Ривз",
                Cast = "Роберт Паттинсон, Зои Кравитц, Пол Дано, Джеффри Райт",
                DurationMinutes = 176, Year = 2022, AgeRating = "16+",
                ImdbRating = 7.8, ReleaseDate = new DateTime(2022, 3, 4),
                PosterUrl = "https://image.tmdb.org/t/p/w500/74xTEgt7R36Fpooo50r9T25onhq.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=mqqft2x_Aa4"
            },
            new Movie
            {
                Title = "Аватар: Путь воды", OriginalTitle = "Avatar: The Way of Water",
                Description = "Джейк Салли живёт с семьёй среди народа Меткайина — морских На'ви. Но прошлое настигает его: враги возвращаются, и ради защиты родных ему придётся покинуть дом.",
                Genre = "Фантастика, Приключения", Director = "Джеймс Кэмерон",
                Cast = "Сэм Уортингтон, Зои Салдана, Сигурни Уивер, Кейт Уинслет",
                DurationMinutes = 192, Year = 2022, AgeRating = "12+",
                ImdbRating = 7.6, ReleaseDate = new DateTime(2022, 12, 16),
                PosterUrl = "https://image.tmdb.org/t/p/w500/t6HIqrRAclMCA60NsSmeqe9RmNV.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=d9MyW72ELq0"
            },
            new Movie
            {
                Title = "Человек-паук: Паутина вселенных", OriginalTitle = "Spider-Man: Across the Spider-Verse",
                Description = "Майлз Моралес отправляется в путешествие по мультивселенной, встречая целую армию Человеков-пауков. Но когда один герой угрожает разрушить всё мироздание, Майлз оказывается перед невозможным выбором.",
                Genre = "Мультфильм, Боевик", Director = "Хоакин Дос Сантос",
                Cast = "Шамейк Мур, Хейли Стайнфельд, Оскар Айзек, Иисса Рей",
                DurationMinutes = 140, Year = 2023, AgeRating = "6+",
                ImdbRating = 8.6, ReleaseDate = new DateTime(2023, 6, 2),
                PosterUrl = "https://image.tmdb.org/t/p/w500/8Vt6mWEReuy4Of61Lnj5Xj704m8.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=cqGjhVJWtEg"
            },
            new Movie
            {
                Title = "Джокер: Безумие на двоих", OriginalTitle = "Joker: Folie à Deux",
                Description = "Артур Флек заключён в психиатрической больнице Аркхэм. Там он встречает Харли Квинн, и их связь разжигает в нём нечто новое — зрелищное, мрачное и неожиданное.",
                Genre = "Триллер, Мюзикл", Director = "Тодд Филлипс",
                Cast = "Хоакин Феникс, Леди Гага, Брендан Глисон, Кэтрин Кинер",
                DurationMinutes = 138, Year = 2024, AgeRating = "18+",
                ImdbRating = 5.5, ReleaseDate = new DateTime(2024, 10, 4),
                PosterUrl = "https://image.tmdb.org/t/p/w500/oEFd7RCJ2LOBL0kGIAolkbKkzXX.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=ENUzLdHJpY4"
            },
            new Movie
            {
                Title = "Фурьёза: Хроники Безумного Макса", OriginalTitle = "Furiosa: A Mad Max Saga",
                Description = "Приквел «Дороги ярости». История становления Фурьёзы — от её похищения из Зелёного места до превращения в легендарного воина пустоши. Жестокий мир, большие ставки и головокружительные погони.",
                Genre = "Боевик, Фантастика", Director = "Джордж Миллер",
                Cast = "Аня Тейлор-Джой, Крис Хемсворт, Том Бёрк",
                DurationMinutes = 148, Year = 2024, AgeRating = "18+",
                ImdbRating = 7.8, ReleaseDate = new DateTime(2024, 5, 24),
                PosterUrl = "https://image.tmdb.org/t/p/w500/iADOJ8Zymht2JPMoy3R7xceZprc.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=XJMuhwVlca4"
            },
            new Movie
            {
                Title = "Кунг-фу Панда 4", OriginalTitle = "Kung Fu Panda 4",
                Description = "По приходит время стать духовным лидером Долины мира и выбрать нового Воина-Дракона. Но сначала ему придётся победить новую злодейку — Хамелеона, способную копировать любой стиль кунг-фу.",
                Genre = "Мультфильм, Приключения", Director = "Майк Митчелл",
                Cast = "Джек Блэк, Вайола Дэвис, Брайан Крэнстон, Аквафина",
                DurationMinutes = 94, Year = 2024, AgeRating = "6+",
                ImdbRating = 6.8, ReleaseDate = new DateTime(2024, 3, 8),
                PosterUrl = "https://image.tmdb.org/t/p/w500/kDp1vUBnMpe8ak4rjgl3cLELqjU.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=0dX4BkOjOkA"
            },
            new Movie
            {
                Title = "Конклав", OriginalTitle = "Conclave",
                Description = "После смерти папы римского кардиналы со всего мира собираются на тайный конклав, чтобы избрать преемника. Но в ходе выборов всплывают скандальные тайны, способные потрясти всю католическую церковь.",
                Genre = "Триллер, Драма", Director = "Эдвард Бергер",
                Cast = "Рэйф Файнс, Стэнли Туччи, Джон Литгоу, Изабелла Росселлини",
                DurationMinutes = 120, Year = 2024, AgeRating = "12+",
                ImdbRating = 7.4, ReleaseDate = new DateTime(2024, 11, 1),
                PosterUrl = "https://image.tmdb.org/t/p/w500/m5WFbEPgO6bQGiXbxvPfqF59KcS.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=BFYS1JodMiA"
            },
            new Movie
            {
                Title = "Субстанция", OriginalTitle = "The Substance",
                Description = "Стареющая голливудская звезда соглашается на участие в экспериментальном медицинском препарате, который создаёт молодую версию неё самой. Но чем больше она использует субстанцию, тем страшнее становятся последствия.",
                Genre = "Ужасы, Фантастика", Director = "Корали Фаржа",
                Cast = "Деми Мур, Маргарет Куэлли, Деннис Куэйд",
                DurationMinutes = 140, Year = 2024, AgeRating = "18+",
                ImdbRating = 7.4, ReleaseDate = new DateTime(2024, 9, 20),
                PosterUrl = "https://image.tmdb.org/t/p/w500/lqoMzCcZYEFK729d6qzt349fB4o.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=QeQQbVsGalY"
            },
            new Movie
            {
                Title = "Лило и Стич", OriginalTitle = "Lilo & Stitch",
                Description = "Ремейк культового мультфильма Disney. Маленькая гавайская девочка Лило усыновляет необычного пришельца, назвав его Стичем. Их дружба изменит жизни обоих — и всей Земли заодно.",
                Genre = "Приключения, Семейный", Director = "Дин Флейшер Кэмп",
                Cast = "Матеа Уайтхорс-Фонсека, Сидни Агудонг, Зак Гальфианакис",
                DurationMinutes = 108, Year = 2025, AgeRating = "6+",
                ImdbRating = 7.1, ReleaseDate = new DateTime(2025, 5, 23),
                PosterUrl = "https://image.tmdb.org/t/p/w500/4YhblVJcRKJqoMDWkQibGBgz5c5.jpg",
                TrailerUrl = "https://www.youtube.com/watch?v=v6TqrLGlHOM"
            },
        };
        context.Movies.AddRange(movies);
        context.SaveChanges();

        var halls = new[] { hall1, hall2, hall3, hall4 };
        var now = DateTime.Today;
        var sessions = new List<Session>();
        var rng = new Random(42);
        foreach (var movie in movies)
        {
            for (int d = 0; d < 7; d++)
            {
                var times = new[] { 10, 13, 16, 19, 22 };
                foreach (var t in times.Take(rng.Next(2, 5)))
                {
                    var hall = halls[rng.Next(halls.Length)];
                    sessions.Add(new Session
                    {
                        Movie = movie,
                        Hall = hall,
                        StartTime = now.AddDays(d).AddHours(t),
                        EndTime = now.AddDays(d).AddHours(t).AddMinutes(movie.DurationMinutes + 20),
                        Format = rng.Next(3) switch { 0 => "IMAX", 1 => "3D", _ => "2D" },
                        BasePrice = 300m + rng.Next(0, 5) * 50m
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
                    var type = (r >= session.Hall.TotalRows - 2) ? SeatType.VIP
                             : (r >= session.Hall.TotalRows - 4) ? SeatType.Sofa
                             : SeatType.Standard;
                    seats.Add(new Seat
                    {
                        Hall = session.Hall,
                        Session = session,
                        Row = rows[r].ToString(),
                        Number = n,
                        Status = rng.Next(6) == 0 ? SeatStatus.Occupied : SeatStatus.Available,
                        Type = type,
                        PriceModifier = type == SeatType.VIP ? 1.8m : type == SeatType.Sofa ? 1.4m : 1.0m
                    });
                }
            }
            context.Seats.AddRange(seats);
        }

        var adminUser = new User
        {
            FullName = "Алексей Морозов",
            Email = "user@cinema.ru",
            PasswordHash = HashPassword("password123"),
            Phone = "+7 (900) 123-45-67",
            LoyaltyPoints = 1240,
            LoyaltyLevel = "Синема Клуб",
            IsAdmin = false
        };
        context.Users.Add(adminUser);
        context.SaveChanges();
    }

    public static string HashPassword(string password)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "_hashed"));
    }

    public static bool CheckPassword(string password, string hash)
    {
        return hash == HashPassword(password);
    }
}

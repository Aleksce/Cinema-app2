using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CinemaApp.Services;

// ── TMDB JSON models ───────────────────────────────────────────────

public record TmdbMovieListResponse(
    [property: JsonPropertyName("results")] List<TmdbMovieItem> Results,
    [property: JsonPropertyName("total_pages")] int TotalPages);

public record TmdbMovieItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("original_title")] string OriginalTitle,
    [property: JsonPropertyName("overview")] string Overview,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("genre_ids")] List<int> GenreIds,
    [property: JsonPropertyName("vote_average")] double VoteAverage,
    [property: JsonPropertyName("release_date")] string ReleaseDate,
    [property: JsonPropertyName("adult")] bool Adult);

public record TmdbMovieDetails(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("original_title")] string OriginalTitle,
    [property: JsonPropertyName("overview")] string Overview,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
    [property: JsonPropertyName("vote_average")] double VoteAverage,
    [property: JsonPropertyName("release_date")] string ReleaseDate,
    [property: JsonPropertyName("runtime")] int? Runtime,
    [property: JsonPropertyName("genres")] List<TmdbGenre> Genres,
    [property: JsonPropertyName("credits")] TmdbCredits? Credits,
    [property: JsonPropertyName("videos")] TmdbVideosWrapper? Videos,
    [property: JsonPropertyName("adult")] bool Adult);

public record TmdbGenre(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);

public record TmdbCredits(
    [property: JsonPropertyName("cast")] List<TmdbCastMember> Cast,
    [property: JsonPropertyName("crew")] List<TmdbCrewMember> Crew);

public record TmdbCastMember(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("character")] string Character,
    [property: JsonPropertyName("profile_path")] string? ProfilePath,
    [property: JsonPropertyName("order")] int Order);

public record TmdbCrewMember(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("job")] string Job);

public record TmdbVideosWrapper(
    [property: JsonPropertyName("results")] List<TmdbVideo> Results);

public record TmdbVideo(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("site")] string Site,
    [property: JsonPropertyName("type")] string Type);

// ── Service ─────────────────────────────────────────────────────────

public class TmdbService : IDisposable
{
    private const string ApiKey  = "42a2ec31887cd90e5f695ba9c377ad17";
    private const string BaseUrl = "https://api.themoviedb.org/3";

    public const string PosterBase   = "https://image.tmdb.org/t/p/w500";
    public const string BackdropBase = "https://image.tmdb.org/t/p/w1280";
    public const string ActorBase    = "https://image.tmdb.org/t/p/w185";

    private readonly HttpClient _http;

    public TmdbService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    private string Url(string path, string extra = "") =>
        $"{BaseUrl}{path}?api_key={ApiKey}&language=ru-RU{extra}";

    public async Task<List<TmdbMovieItem>> GetNowPlayingAsync(int pages = 3)
    {
        var all = new List<TmdbMovieItem>();
        for (int p = 1; p <= pages; p++)
        {
            var resp = await _http.GetFromJsonAsync<TmdbMovieListResponse>(
                Url("/movie/now_playing", $"&page={p}&region=RU"));
            if (resp?.Results == null) break;
            all.AddRange(resp.Results);
            if (p >= resp.TotalPages) break;
        }
        return all.Where(m => !m.Adult).ToList();
    }

    public async Task<List<TmdbMovieItem>> GetUpcomingAsync(int pages = 2)
    {
        var all = new List<TmdbMovieItem>();
        for (int p = 1; p <= pages; p++)
        {
            var resp = await _http.GetFromJsonAsync<TmdbMovieListResponse>(
                Url("/movie/upcoming", $"&page={p}&region=RU"));
            if (resp?.Results == null) break;
            all.AddRange(resp.Results);
            if (p >= resp.TotalPages) break;
        }
        return all.Where(m => !m.Adult).ToList();
    }

    public async Task<TmdbMovieDetails?> GetDetailsAsync(int tmdbId)
    {
        try
        {
            return await _http.GetFromJsonAsync<TmdbMovieDetails>(
                Url($"/movie/{tmdbId}", "&append_to_response=credits,videos"));
        }
        catch { return null; }
    }

    // ── Helpers ───────────────────────────────────────────────────

    public string? GetTrailerUrl(TmdbMovieDetails details)
    {
        var v = details.Videos?.Results
            .FirstOrDefault(v => v.Site == "YouTube" && (v.Type == "Trailer" || v.Type == "Teaser"));
        return v != null ? $"https://www.youtube.com/watch?v={v.Key}" : null;
    }

    public string? GetDirector(TmdbMovieDetails details)
        => details.Credits?.Crew.FirstOrDefault(c => c.Job == "Director")?.Name;

    public string GetCast(TmdbMovieDetails details, int max = 5)
    {
        var cast = details.Credits?.Cast
            .OrderBy(c => c.Order).Take(max).Select(c => c.Name).ToList() ?? new();
        return string.Join(", ", cast);
    }

    /// <summary>Returns JSON array of top cast with photo URLs for on-screen display.</summary>
    public string GetTopCastJson(TmdbMovieDetails details, int max = 8)
    {
        var cast = details.Credits?.Cast
            .OrderBy(c => c.Order)
            .Take(max)
            .Select(c => new
            {
                name      = c.Name,
                character = c.Character,
                photoUrl  = c.ProfilePath != null ? ActorBase + c.ProfilePath : string.Empty
            })
            .ToList() ?? new();

        return JsonSerializer.Serialize(cast);
    }

    public string? GetBackdropUrl(TmdbMovieDetails details)
        => details.BackdropPath != null ? BackdropBase + details.BackdropPath : null;

    public string GetAgeRating(bool adult) => adult ? "18+" : "12+";

    public void Dispose() => _http.Dispose();
}

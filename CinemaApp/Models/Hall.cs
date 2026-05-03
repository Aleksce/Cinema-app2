namespace CinemaApp.Models;

public class Hall
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SeatsPerRow { get; set; }

    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}

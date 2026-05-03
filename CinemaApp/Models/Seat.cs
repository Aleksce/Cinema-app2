namespace CinemaApp.Models;

public enum SeatStatus
{
    Available,
    Selected,
    Occupied
}

public enum SeatType
{
    Standard,
    VIP,
    Sofa
}

public class Seat
{
    public int Id { get; set; }
    public int HallId { get; set; }
    public Hall Hall { get; set; } = null!;
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public SeatType Type { get; set; } = SeatType.Standard;
    public decimal PriceModifier { get; set; } = 1.0m;
}

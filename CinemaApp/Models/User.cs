namespace CinemaApp.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; } = 0;
    public string LoyaltyLevel { get; set; } = "Стандарт";
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; } = false;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

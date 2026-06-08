namespace PhoneBot;

public class UserUsage
{
    public int CountToday { get; set; } = 0;
    public DateTime LastResetDate { get; set; } = DateTime.UtcNow.Date;
}

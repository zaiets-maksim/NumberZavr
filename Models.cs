namespace PhoneBot;

public class PhoneRecord
{
    public string Number { get; set; } = "";
    public int TotalIssued { get; set; } = 0;
}

public class UserUsage
{
    public long UserId { get; set; }
    public int CountToday { get; set; } = 0;
    public DateTime LastResetDate { get; set; } = DateTime.UtcNow.Date;
}

public class BotData
{
    public List<PhoneRecord> Phones { get; set; } = [];
    public List<UserUsage> UserUsages { get; set; } = [];
}

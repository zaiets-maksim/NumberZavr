using System;
using System.Collections.Generic;

namespace NumberZavr;

public class BotState
{
    public int CurrentPhoneIndex { get; set; }
    public int CurrentPhoneUsage { get; set; }
    public Dictionary<string, DateTime> PhoneLastUsed { get; set; } = new();
    public HashSet<long> ActiveUsers { get; set; } = new();
}
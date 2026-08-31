namespace SecurityGateway.Application.Blocking;

public sealed class AutomaticBlockingOptions
{
    public const string SectionName = "AutomaticBlocking";

    public bool Enabled { get; set; } = true;
    public int CriticalThreshold { get; set; } = 80;
    public int HighThreshold { get; set; } = 60;
    public int MediumThreshold { get; set; } = 40;
    public int CriticalBlockDurationMinutes { get; set; } = 1440;
    public int HighBlockDurationMinutes { get; set; } = 240;
    public int MediumBlockDurationMinutes { get; set; } = 30;
}

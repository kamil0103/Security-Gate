namespace SecurityGateway.Application.BehavioralAnalysis;

public sealed class BehavioralAnalysisOptions
{
    public const string SectionName = "BehavioralAnalysis";

    public bool Enabled { get; set; }
    public int WindowSeconds { get; set; } = 60;
    public int BurstThreshold { get; set; } = 100;
}

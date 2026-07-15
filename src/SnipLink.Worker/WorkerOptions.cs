namespace SnipLink.Worker;

public class WorkerOptions
{
    public const string SectionName = "Worker";

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(2);

    public bool PruneOldClicks { get; set; } = false;

    public int RetentionDays { get; set; } = 90;
}

namespace SnipLink.Worker;

public class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>How often to roll clicks up into daily stats.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>When true, delete raw ClickEvent rows older than <see cref="RetentionDays"/> after aggregation.</summary>
    public bool PruneOldClicks { get; set; } = false;

    /// <summary>Raw clicks older than this many days are pruned (only if pruning is enabled).</summary>
    public int RetentionDays { get; set; } = 90;
}

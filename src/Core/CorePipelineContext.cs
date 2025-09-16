using Michelangelo.Types;

namespace Core;

public class CorePipelineContext : Context
{
    public required string RepositoryUrl { get; init; }
    public required string WorkingDirectory { get; init; }

    // Results/state shared between jobs
    public bool CloneSucceeded { get; set; }
    public DateTime? CloneCompletedAt { get; set; }
    public DiagnosticsLog Diagnostics { get; } = new();

    public void Info(string message) => Diagnostics.Info(message);
    public void Warn(string message) => Diagnostics.Warn(message);
    public void Error(string message) => Diagnostics.Error(message);
}

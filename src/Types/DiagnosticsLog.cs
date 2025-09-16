namespace Michelangelo.Types;

/// <summary>
/// Structured diagnostics collection with timestamp, level and message.
/// </summary>
public sealed class DiagnosticsLog
{
    private readonly List<DiagnosticsEntry> _entries = new();
    public IReadOnlyList<DiagnosticsEntry> Entries => _entries;

    public void Info(string message) => Add(DiagnosticsLevel.Info, message);
    public void Warn(string message) => Add(DiagnosticsLevel.Warning, message);
    public void Error(string message) => Add(DiagnosticsLevel.Error, message);

    public void Add(DiagnosticsLevel level, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        _entries.Add(new DiagnosticsEntry(DateTime.UtcNow, level, message));
    }

    public void Append(DiagnosticsLog other)
    {
        if (other == null) return;
        foreach (var e in other.Entries)
        {
            _entries.Add(e);
        }
    }

    public override string ToString() => string.Join(Environment.NewLine, _entries.Select(e => e.ToString()));
}

public enum DiagnosticsLevel { Info, Warning, Error }

public readonly record struct DiagnosticsEntry(DateTime TimestampUtc, DiagnosticsLevel Level, string Message)
{
    public override string ToString() => $"[{TimestampUtc:O}][{Level}] {Message}";
}
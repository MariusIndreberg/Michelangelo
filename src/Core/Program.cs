using Core;
using Michelangelo.Types;

if (args.Length == 0)
{
    Console.WriteLine("Usage: core <git-repo-url> [--keep]");
    return;
}

var repoUrl = args[0];
var keep = args.Skip(1).Any(a => a.Equals("--keep", StringComparison.OrdinalIgnoreCase));
// Use project-local tmp directory instead of global system temp.
var projectRoot = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!; // navigate from bin/Debug/net9.0
var tmpRoot = Path.Combine(projectRoot.FullName, "tmp");
Directory.CreateDirectory(tmpRoot);
string SafeRepoFolder(string url)
{
    // derive a safe folder name from repo URL (owner_repo_hashprefix)
    try
    {
        var uri = new Uri(url);
        var segments = uri.Segments.Last().TrimEnd('/');
        if (segments.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) segments = segments[..^4];
        var hostPart = uri.Host.Replace('.', '_');
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(url)))[..8];
        return $"{hostPart}_{segments}_{hash}";
    }
    catch
    {
        return "repo_" + Guid.NewGuid().ToString("N");
    }
}
string UniqueWorkingDirectory(string baseName)
{
    var basePath = Path.Combine(tmpRoot, baseName);
    if (!Directory.Exists(basePath)) return basePath;
    int i = 1;
    while (true)
    {
        var candidate = basePath + "_" + i.ToString("D2");
        if (!Directory.Exists(candidate)) return candidate;
        i++;
    }
}
var tempDir = UniqueWorkingDirectory(SafeRepoFolder(repoUrl));
var ctx = new CorePipelineContext { RepositoryUrl = repoUrl, WorkingDirectory = tempDir };

Console.WriteLine("[Runner] Starting jobs (monadic chain)...");
var cancel = CancellationToken.None;
var result = await ctx
    .RunContextJob<CorePipelineContext>(new CloneJob(), cancel)
    .ThenContextJob(c => new CleanupJob(skipDeletion: keep), cancel);

if (!result.Success)
{
    Console.WriteLine("[Runner] Pipeline failed: " + (result.Error?.Message ?? "unknown error"));
    Environment.ExitCode = 1;
}
else
{
    Console.WriteLine("[Runner] Pipeline succeeded.");
}

Console.WriteLine();
Console.WriteLine("=== Pipeline Summary ===");
Console.WriteLine($"Repository: {ctx.RepositoryUrl}");
Console.WriteLine($"Working Directory: {ctx.WorkingDirectory}");
Console.WriteLine($"Clone Succeeded: {ctx.CloneSucceeded}");
Console.WriteLine($"Clone Completed (UTC): {ctx.CloneCompletedAt?.ToString("O") ?? "<none>"}");
var entries = ctx.Diagnostics.Entries;
if (entries.Count > 0)
{
    Console.WriteLine("Diagnostics:");
    foreach (var e in entries)
        Console.WriteLine(" - " + e.ToString());
}
else
{
    Console.WriteLine("No diagnostics recorded.");
}

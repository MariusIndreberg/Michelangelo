using Core.Pipelines;

if (args.Length == 0)
{
    Console.WriteLine("Usage: core <git-repo-url> [--keep]");
    return;
}

var repoUrl = args[0];
var keep = args.Skip(1).Any(a => a.Equals("--keep", StringComparison.OrdinalIgnoreCase));

// Delegate execution to the consolidated MainPipeline entry point
var exit = await MainPipeline.RunAsync(repoUrl, keep, CancellationToken.None);
Environment.ExitCode = exit;

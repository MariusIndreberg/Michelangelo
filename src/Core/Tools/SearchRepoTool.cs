using System.Text.RegularExpressions;
using LlmLib;
using LlmLib.Substrate.Model.ChatCompletion;
using Core;

namespace Tools;

public class SearchRepoTool
{
	private static readonly Regex AttributeRouteRegex = new("\\[(Http(Get|Post|Put|Delete|Patch|Head|Options))(\\(\"?(?<route>[^\"\\)]*)\"?\\))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex MinimalApiRegex = new("\\b(MapGet|MapPost|MapPut|MapDelete|MapPatch|MapMethods)\\s*\\(\\s*\"(?<route>[^\"]+)\"", RegexOptions.Compiled);

	public async Task<IReadOnlyList<EndpointInfo>> FindEndpointsAsync(string rootDirectory, CancellationToken cancellationToken = default)
	{
		var candidates = new List<EndpointInfo>();
		if (!Directory.Exists(rootDirectory)) return candidates;

		foreach (var file in Directory.EnumerateFiles(rootDirectory, "*.cs", SearchOption.AllDirectories))
		{
			if (file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) ||
				file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
				continue;

			int lineNumber = 0;
			foreach (var line in File.ReadLines(file))
			{
				cancellationToken.ThrowIfCancellationRequested();
				lineNumber++;
				var attrMatch = AttributeRouteRegex.Match(line);
				if (attrMatch.Success)
				{
					var method = attrMatch.Groups[1].Value; // HttpGet
					var route = attrMatch.Groups["route"].Success ? attrMatch.Groups["route"].Value : string.Empty;
					candidates.Add(new EndpointInfo
					{
						HttpMethod = method.Replace("Http", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant(),
						Route = route,
						SourceFile = file,
						LineNumber = lineNumber
					});
					continue;
				}
				var minimalMatch = MinimalApiRegex.Match(line);
				if (minimalMatch.Success)
				{
					var map = minimalMatch.Groups[1].Value; // MapGet
					var route = minimalMatch.Groups["route"].Value;
					candidates.Add(new EndpointInfo
					{
						HttpMethod = map.Replace("Map", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant(),
						Route = route,
						SourceFile = file,
						LineNumber = lineNumber
					});
				}
			}
		}

		// Use LLM to enrich / validate endpoints (batch prompt)
		if (candidates.Count == 0) return candidates;

		var prompt = BuildClassificationPrompt(candidates, rootDirectory);
		var request = new ModelRequest
		{
			Messages = new[]
			{
				new ModelRequestMessage { Role = "system", Content = "You analyze C# code endpoints. Return JSON only."},
				new ModelRequestMessage { Role = "user", Content = prompt }
			},
			MaxTokens = 800,
			Temperature = 0.1,
			N = 1,
			Stream = false
		};

		var response = await LLM.SendRequestAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
	if (response?.Choices is { Length: > 0 })
		{
			var content = response.Choices.First().Message?.Content;
			if (!string.IsNullOrWhiteSpace(content))
			{
				TryMergeJson(content, candidates);
			}
		}

		return candidates;
	}

	private static string BuildClassificationPrompt(IReadOnlyList<EndpointInfo> endpoints, string root)
	{
		var sb = new System.Text.StringBuilder();
		sb.AppendLine("Given the following extracted C# endpoints (method, route, file, line), validate and if needed infer missing route segments.");
		sb.AppendLine("Return strict JSON array with objects: { method, route, summary? }. No code fences.");
		sb.AppendLine("Root: " + root);
		sb.AppendLine("Endpoints:");
		foreach (var e in endpoints.Take(100)) // cap
		{
			sb.AppendLine($"- method={e.HttpMethod}; route={e.Route}; file={Path.GetFileName(e.SourceFile)}; line={e.LineNumber}");
		}
		if (endpoints.Count > 100) sb.AppendLine($"(Truncated list, total={endpoints.Count})");
		return sb.ToString();
	}

	private static void TryMergeJson(string raw, List<EndpointInfo> endpoints)
	{
		try
		{
			// Attempt to find first '[' and last ']' to extract JSON array
			int start = raw.IndexOf('[');
			int end = raw.LastIndexOf(']');
			if (start < 0 || end <= start) return;
			var json = raw[start..(end + 1)];
			var enriched = System.Text.Json.JsonSerializer.Deserialize<List<EndpointJson>>(json);
			if (enriched == null) return;
			// Simple merge: match by method+route (case-insensitive) and add summary
			foreach (var item in enriched)
			{
				var match = endpoints.FirstOrDefault(e => string.Equals(e.HttpMethod, item.method, StringComparison.OrdinalIgnoreCase) && string.Equals(e.Route, item.route, StringComparison.OrdinalIgnoreCase));
				if (match != null && !string.IsNullOrWhiteSpace(item.summary))
				{
					match.Summary = item.summary;
				}
			}
		}
		catch
		{
			// Swallow parsing issues; keep raw heuristic endpoints
		}
	}

	private sealed class EndpointJson
	{
		public string method { get; set; } = string.Empty;
		public string route { get; set; } = string.Empty;
		public string? summary { get; set; }
	}
}
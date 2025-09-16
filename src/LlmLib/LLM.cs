using LlmLib.Substrate.Model.ChatCompletion;

namespace LlmLib;

public class LLM
{
    public static async Task<ModelResponse?> SendRequestAsync(ModelRequest request, bool useDAT = false, string? modelName = null, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        var client = new Substrate.SubstrateLlmApiClient(httpClient, type: modelName);
        return await client.SendRequestAsync(request, useDAT, cancellationToken).ConfigureAwait(false);
    }
}

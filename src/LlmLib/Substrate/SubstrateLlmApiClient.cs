// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubstrateLlmApiClient.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LlmLib.Substrate
{
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json;
    using LlmLib.Providers;
    using LlmLib.Substrate.Model.ChatCompletion;

    /// <summary>
    /// Client for interacting with the Substrate LLM API.
    /// </summary>
    public class SubstrateLlmApiClient
    {
        private const string Endpoint = "https://fe-26.qas.bing.net/sdf/chat/completions";
        private static JsonSerializerOptions serializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        // Available models are listed here: https://msasg.visualstudio.com/QAS/_wiki/wikis/QAS.wiki/134728/Getting-Started-with-Substrate-LLM-API?anchor=available-models
        private readonly string defaultChatCompletionsModelType = "dev-gpt-41-shortco-2025-04-14";
        private readonly string modelType;
        private string? scenarioGuid;
        private HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubstrateLlmApiClient"/> class.
        /// </summary>
        /// <param name="http">The HTTP client to use for requests.</param>
        /// <param name="type">The model type to use for requests.</param>
        /// <param name="scenarioGUID">The GUID for the scenario.</param>
        public SubstrateLlmApiClient(HttpClient http, string? type = null, string? scenarioGUID = null)
        {
            this.httpClient = http;
            this.modelType = type ?? this.defaultChatCompletionsModelType;
            this.scenarioGuid = scenarioGUID;
        }

        /// <summary>
        /// Sends a request to the Substrate LLM API.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="useDAT">use DAT tool in test environment.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response from the API.</returns>
        public async Task<ModelResponse?> SendRequestAsync(ModelRequest request, bool useDAT = false, CancellationToken cancellationToken = default)
        {
            var requestData = JsonSerializer.Serialize(request, serializerOptions);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            httpRequest.Content = new StringContent(requestData, Encoding.UTF8, "application/json");

            if (useDAT)
            {
                var token = await TokenProvider.GetTokenAsync().ConfigureAwait(false);
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            httpRequest.Headers.Add("X-ModelType", this.modelType);

            if (this.scenarioGuid != null)
            {
                httpRequest.Headers.Add("X-ScenarioGUID", this.scenarioGuid);
            }

            var httpResponse = await this.httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            var str = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return await httpResponse.Content.ReadFromJsonAsync<ModelResponse>(cancellationToken).ConfigureAwait(false);
        }
    }
}

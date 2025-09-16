// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubstrateLlmExecutionSettings.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace LlmLib.Substrate
{
    internal sealed class SubstrateLlmExecutionSettings
    {
        public SubstrateLlmExecutionSettings(
            double temperature = 0.3,
            int maxTokens = 4000,
            int n = 1,
            float topP = 1.0f,
            string[]? stop = null)
        {
            this.Temperature = temperature;
            this.MaxTokens = maxTokens;
            this.N = n;
            this.Stop = stop;
            this.TopP = topP;
        }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0;

        [JsonPropertyName("top_p")]
        public float TopP { get; set; } = 1;

        [JsonPropertyName("n")]
        public int N { get; set; } = 1;

        [JsonPropertyName("stop")]
        public string[]? Stop { get; set; }
    }
}

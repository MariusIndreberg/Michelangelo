// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModelRequest.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LlmLib.Substrate.Model.ChatCompletion
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Object to be serialized and sent to the LLM API as request body.
    /// See https://msasg.visualstudio.com/QAS/_wiki/wikis/QAS.wiki/134728/Getting-Started-with-Substrate-LLM-API.
    /// </summary>
    public class ModelRequest
    {
        /// <summary>
        /// Messages.
        /// </summary>
        [JsonPropertyName("messages")]
#pragma warning disable CA1819 // Properties should not return arrays
        public ModelRequestMessage[]? Messages { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Max tokens.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        /// <summary>
        /// Temperature.
        /// </summary>
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0;

        /// <summary>
        /// Top P.
        /// </summary>
        [JsonPropertyName("top_p")]
        public float TopP { get; set; } = 1;

        /// <summary>
        /// N.
        /// </summary>
        [JsonPropertyName("n")]
        public int N { get; set; } = 1;

        /// <summary>
        /// Stop.
        /// </summary>
        [JsonPropertyName("stop")]
#pragma warning disable CA1819 // Properties should not return arrays
        public string[]? Stop { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Stream mode.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Text format field.
        /// </summary>
        [JsonPropertyName("response_format")]
        public FormatText? Text { get; set; }
    }
}

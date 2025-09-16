// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Usage.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LlmLib.Substrate.Model
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the usage details of the language model.
    /// </summary>
    public class Usage
    {
        /// <summary>
        /// Gets or sets the number of tokens used in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens used in the completion.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Gets or sets the total number of tokens used.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}

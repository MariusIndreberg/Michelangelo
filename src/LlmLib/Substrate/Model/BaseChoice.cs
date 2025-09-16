// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseChoice.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LlmLib.Substrate.Model
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a base choice with a finish reason and an index.
    /// </summary>
    public class BaseChoice
    {
        /// <summary>
        /// Gets or sets the reason for finishing.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        /// <summary>
        /// Gets or sets the index of the choice.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}

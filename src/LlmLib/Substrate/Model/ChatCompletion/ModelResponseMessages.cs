// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModelResponseMessages.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LlmLib.Substrate.Model.ChatCompletion
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the response messages from the model.
    /// </summary>
    public class ModelResponseMessages
    {
        /// <summary>
        /// Gets or sets the role of the message.
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FormatText.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace LlmLib.Substrate.Model.ChatCompletion
{
    /// <summary>
    /// Represents the format of the text in a message.
    /// </summary>
    public class FormatText
    {
        /// <summary>
        /// The text of the message.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}

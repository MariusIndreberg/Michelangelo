// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Choice.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LlmLib.Substrate.Model.ChatCompletion
{
    using System.Text.Json.Serialization;
    using LlmLib.Substrate.Model;

    /// <summary>
    /// Represents a choice in the chat completion model.
    /// </summary>
    public class Choice : BaseChoice
    {
        /// <summary>
        /// Gets or sets the message associated with the choice.
        /// </summary>
        [JsonPropertyName("message")]
        public ModelResponseMessages? Message { get; set; }
    }
}

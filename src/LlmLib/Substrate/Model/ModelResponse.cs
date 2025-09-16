// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModelResponse.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LlmLib.Substrate.Model
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a response from the model.
    /// </summary>
    /// <typeparam name="TChoice">The type of the choices in the response.</typeparam>
    public class ModelResponse<TChoice>
        where TChoice : BaseChoice
    {
        /// <summary>
        /// Gets or sets the identifier of the response.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the object type of the response.
        /// </summary>
        [JsonPropertyName("object")]
#pragma warning disable CA1720 // Identifier contains type name
        public string? Object { get; set; }
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Gets or sets the creation timestamp of the response.
        /// </summary>
        [JsonPropertyName("created")]
        public int Created { get; set; }

        /// <summary>
        /// Gets or sets the model used for the response.
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the usage information of the response.
        /// </summary>
        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }

        /// <summary>
        /// Gets or sets the choices in the response.
        /// </summary>
        [JsonPropertyName("choices")]
#pragma warning disable CA1819 // Properties should not return arrays
        public TChoice[]? Choices { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets the error information of the response.
        /// </summary>
        [JsonPropertyName("error")]
        public object? Error { get; set; }

        /// <summary>
        /// Gets or sets additional data in the response.
        /// </summary>
        [JsonPropertyName("additionaldata")]
        public Dictionary<string, JsonElement>? AdditionalData { get; set; }
    }
}

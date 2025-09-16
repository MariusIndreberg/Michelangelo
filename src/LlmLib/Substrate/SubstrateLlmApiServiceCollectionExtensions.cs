// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubstrateLlmApiServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LlmLib.Substrate
{
    /// <summary>
    /// Provides extension methods for adding Substrate LLM API chat completion services to the <see cref="IKernelBuilder"/>.
    /// </summary>
    public static class SubstrateLlmApiServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a Substrate LLM API chat completion service to the <see cref="IKernelBuilder"/>.
        /// </summary>
        /// <param name="builder">The kernel builder to add the service to.</param>
        /// <param name="serviceId">The optional service identifier.</param>
        /// <param name="modelId">The optional model identifier.</param>
        /// <param name="httpClient">The optional HTTP client to use for the service.</param>
        /// <returns>The updated kernel builder.</returns>
        public static IKernelBuilder AddSubstrateLlmApiChatCompletion(
            [NotNull] this IKernelBuilder builder,
            string? serviceId = null,
            string? modelId = null,
            HttpClient? httpClient = null)
        {
            Func<IServiceProvider, object?, SubstrateLlmApiChatCompletionService> factory = (serviceProvider, _) =>
            {
                var client = new SubstrateLlmApiClient(
                    httpClient ?? serviceProvider.GetService<HttpClient>() ?? new HttpClient(),
                    modelId);

                return new(client);
            };

            builder.Services.AddKeyedSingleton<IChatCompletionService>(serviceId, factory);

            return builder;
        }
    }
}

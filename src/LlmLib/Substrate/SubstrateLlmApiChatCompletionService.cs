// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubstrateLlmApiChatCompletionService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using LlmLib.Substrate.Model.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LlmLib.Substrate
{
    /// <summary>
    /// Service for handling chat completions using the Substrate LLM API.
    /// </summary>
    public class SubstrateLlmApiChatCompletionService : IChatCompletionService
    {
        private SubstrateLlmApiClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubstrateLlmApiChatCompletionService"/> class.
        /// </summary>
        /// <param name="client">The Substrate LLM API client.</param>
        public SubstrateLlmApiChatCompletionService(SubstrateLlmApiClient client)
        {
            this.client = client;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
    ChatHistory chatHistory,
    PromptExecutionSettings? executionSettings = null,
    Microsoft.SemanticKernel.Kernel? kernel = null,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(chatHistory);

            var requestMessages = new ModelRequestMessage[chatHistory.Count];
            for (int i = 0; i < chatHistory.Count; i++)
            {
                ChatMessageContent? chatMessage = chatHistory[i];
                requestMessages[i] = new ModelRequestMessage
                {
                    Role = chatMessage.Role.Label,
                    Content = chatMessage.Content,
                };
            }

            SubstrateLlmExecutionSettings settings = CreateExecutionSettings(executionSettings);

            var response = await this.client.SendRequestAsync(
                new ModelRequest
                {
                    Temperature = settings.Temperature,
                    MaxTokens = settings.MaxTokens,
                    N = settings.N,
                    TopP = settings.TopP,
                    Stop = settings.Stop,
                    Messages = requestMessages,
                    Stream = false,
                },
                true,
                cancellationToken).ConfigureAwait(false);

            if (response == null || response.Choices is null)
            {
                if (response?.Error != null)
                {
                    throw new IQALanguageModelException();
                }

                return Array.Empty<ChatMessageContent>();
            }

            var chatMessageContents = new List<ChatMessageContent>();
            foreach (var message in response.Choices)
            {
                chatMessageContents.Add(new ChatMessageContent(new AuthorRole(message!.Message!.Role!), message!.Message!.Content!));
            }

            return chatMessageContents;
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Microsoft.SemanticKernel.Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private static SubstrateLlmExecutionSettings CreateExecutionSettings(PromptExecutionSettings? executionSettings)
        {
            var serializedSettings = JsonSerializer.Serialize(executionSettings);
            var deserialized = JsonSerializer.Deserialize<SubstrateLlmExecutionSettings>(serializedSettings);
            return deserialized ?? new SubstrateLlmExecutionSettings();
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using LlmLib;
using LlmLib.Substrate.Model.ChatCompletion;
using Xunit;

namespace Core.Tests;

public class LlmLibTest
{

    private static ModelRequest CreateChatCompletionModelRequest(bool stream = false)
    {
        return new ModelRequest
        {
            Messages = new ModelRequestMessage[]
          {
            new ModelRequestMessage
            {
                Role = "system",
                Content = "You are an helpful assistant",
            },
            new ModelRequestMessage
            {
                Role = "user",
                Content = "Tell me a joke about cats."
            },
          },
            MaxTokens = 100,
            Temperature = 0,
            TopP = 1,
            N = 1,
            Stop = null,
            Stream = stream
        };
    }


    [Fact(Skip = "Integration test, requires network access and valid token")]
    public async Task LlmTest()
    {
        var request = CreateChatCompletionModelRequest();
        var result = await LLM.SendRequestAsync(request, true, null, CancellationToken.None);
    }
}
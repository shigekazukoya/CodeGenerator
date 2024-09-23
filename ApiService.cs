using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CodeGenerator
{
    public class ApiService
    {
        private string geminiModelId = "gemini-1.5-flash";
        private Kernel kernel;
        private IChatCompletionService chatCompletionService;
        public string SystemMessage = """
    For each code block, include the filename in the code block as shown below:

    ```language:filename.extension
    code
    ```
    """;

        public ApiService(string apiKey)
        {
#pragma warning disable SKEXP0070
            this.kernel = Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion(
                    modelId: geminiModelId,
                    apiKey: apiKey)
                .Build();
#pragma warning restore SKEXP0070

            chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> GenerateCodeWithGemini(string prompt, string language, string fileContent = null)
        {
            string newPrompt;

            if (!string.IsNullOrEmpty(fileContent))
            {

                newPrompt = $"""
    Based on the following {language} file content, generate the output.

    File content:
    {fileContent}

    Additional prompt:
    {prompt}
    """;
            }
            else
            {
                newPrompt = $"""
Generate {language} code for the following prompt:

{prompt}
""";
            }

            var chatHistory = new ChatHistory(SystemMessage);
            chatHistory.AddUserMessage(newPrompt);

            OpenAIPromptExecutionSettings settings = new()
            {
                MaxTokens = 100,
            };
            var reply = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings);
            chatHistory.Add(reply);
            if (string.IsNullOrEmpty(reply.Content))
            {
                return string.Empty;
            }
            else
            {
                return reply.Content;
            }
        }
    }
}

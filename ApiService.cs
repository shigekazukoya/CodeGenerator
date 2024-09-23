using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator
{
    public class ApiService
    {
        private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
        private string ApiKey;

        public ApiService(string apiKey)
        {
            ApiKey = apiKey;
        }

        public async Task<string> GenerateCodeWithGemini(string prompt, string language, string fileContent = null)
        {
            using (var client = new HttpClient())
            {
                string newPrompt;

                if (!string.IsNullOrEmpty(fileContent))
                {
                    newPrompt = $"Based on the following {language} file content, generate the output. For each code block, include the filename in the code block as shown below:\n\n```langualge:filename.extension\ncode\n```\n\nFile content:\n{fileContent}\n\nAdditional prompt:\n{prompt}";
                }
                else
                {
                    newPrompt = $"Generate {language} code for the following prompt. For each code block, include the filename in the code block as shown below:\n\n```langualge:filename.extension\ncode\n```\n\nPrompt:\n{prompt}";
                }

                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = newPrompt }
                            }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Add("x-goog-api-key", ApiKey);

                var response = await client.PostAsync($"{API_URL}", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed: {responseString}");
                }

                var responseObject = JObject.Parse(responseString);
                return responseObject["candidates"][0]["content"]["parts"][0]["text"].ToString();
            }
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppToDB.Abstractions;

namespace OllamaLlmPlugin
{
    using Microsoft.Extensions.Options;
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.ChatCompletion;
    using System.Net.Http.Headers;
    using System.Text.Json;

    public class OllamaProviderNoTools 
    {
        public string Name => "Ollama";
        string apiKey = "f098f98eac994481a23fa98a121a4365.wCBtU7dObLyS5b3p2H7tkOKK";

        public bool SupportsKernel => false;
        HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://ollama.com/api")
        };
        public async Task<string> GenerateAsync(
            ChatHistory history,
            string model)
        {
            var prompt = string.Join("\n",
                history.Select(x => $"{x.Role}: {x.Content}"));

            var body = new
            {
                model = model,
                prompt = prompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(body);

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://ollama.com/api/generate");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer", apiKey);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var text = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(text);

            return doc.RootElement
                .GetProperty("response")
                .GetString() ?? "";
        }

        public List<string> GetModels()
        {
            return new()
        {
            "gpt-oss:120b-cloud",
            "gemma4:31b-cloud",
            "devstral-2:123b-cloud"
        };
        }

        public void Register(
            IKernelBuilder builder,
            IServiceProvider sp,
            string model)
        {
            var apiKey = "f098f98eac994481a23fa98a121a4365.wCBtU7dObLyS5b3p2H7tkOKK";
           

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);
            builder.AddOpenAIChatCompletion(
                    modelId: model,
                    apiKey: apiKey,
                    httpClient: client);
        }
    }
}

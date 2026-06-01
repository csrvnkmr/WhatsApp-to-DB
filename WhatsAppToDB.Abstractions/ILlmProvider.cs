using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WhatsAppToDB.LlmProviders;

namespace WhatsAppToDB.Abstractions
{
    public interface ILlmProvider
    {
        string Name { get; }

        bool SupportsKernel { get; }

        void Register(IKernelBuilder builder, 
            LlmConfig config,
            string model);

        Task<string> GenerateAsync(
            ChatHistory history,
            string model,
            Kernel kernel);

        Task<string> GenerateAsync(
            ChatHistory history,
            string model,
            Kernel kernel,
            CancellationToken cancellationToken = default)
        {
            return GenerateAsync(history, model, kernel);
        }
    }
}

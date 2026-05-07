using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppToDB.Abstractions
{
    public interface ILlmProvider
    {
        string Name { get; }

        List<string> GetModels();

        bool SupportsKernel { get; }

        void Register(
            IKernelBuilder builder,
            IServiceProvider sp,
            string model);

        Task<string> GenerateAsync(
            ChatHistory history,
            string model);
    }
}

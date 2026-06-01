using Microsoft.SemanticKernel;
using WhatsAppToDB.Abstractions;
using WhatsAppToDB.Data;

namespace WhatsAppToDB.Services
{
    public interface IQueryService
    {
        Task<ChatMessageDto> ExecuteQuery(IServiceScopeFactory scopeFactory,
            IdentityContext identity, string messageText, PromptExecutionSettings? pes, ILogger waLogger,
            ChatDbRepository repo, long sessionid, CancellationToken cancellationToken = default);

        Task<EvalComparisonResult> CompareWithLlm(
            IServiceScopeFactory scopeFactory,
            IdentityContext identity,
            string question,
            string groundTruthJson,
            string llmResultJson,
            CancellationToken cancellationToken = default);
    }
}

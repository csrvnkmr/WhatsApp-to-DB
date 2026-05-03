using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using WhatsAppToDB;
using WhatsAppToDB.Data;
using WhatsAppToDB.Services;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
var metadata = new PluginMetadata();

// Centralize all service configurations in one method for better organization
// this is an extension method in ServiceCollectionExtension.cs
builder.Services.ConfigureAllServices(builder.Configuration, metadata);

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

//await TestSqliteExecution();
//await AdventureWorksTestHarness.RunTestMessages(builder);
//await AdventureWorksTestHarness.RunSecurityTests(builder);

var app = builder.Build();
app.UseCors("AllowAll");
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<ChatDbRepository>();
    await repo.InitializeAsync();
}
app.UseMiddleware<TokenAuthMiddleware>();

app.UseStaticFiles(); // This will serve index.html if it's in a folder named wwwroot

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.Run();


async Task TestSqliteExecution()
{
    var repo = new WhatsAppToDB.Tests.SqliteRepositoryTests();
    await repo.Full_Sqlite_Test_Create_Insert_Select_Delete();
}

 
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using WhatsAppToDB;
using WhatsAppToDB.Data;
using WhatsAppToDB.Services;
using WhatsAppToDB.Settings;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
var metadata = new PluginMetadata();

// Centralize all service configurations in one method for better organization
// this is an extension method in ServiceCollectionExtension.cs
builder.Services.ConfigureAllServices(builder.Configuration, metadata);
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".InsightChat.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
//await TestSqliteExecution();
//await AdventureWorksTestHarness.RunTestMessages(builder);
//await AdventureWorksTestHarness.RunSecurityTests(builder);

var app = builder.Build();
app.UseRouting();
app.UseCors("AllowAll");
app.UseSession();


app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles(); // This will serve index.html if it's in a folder named wwwroot

app.UseMiddleware<TokenAuthMiddleware>();
app.UseMiddleware<SessionBootstrapMiddleware>();

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<ChatDbRepository>();
    await repo.InitializeAsync();
}
app.Run();


async Task TestSqliteExecution()
{
    var repo = new WhatsAppToDB.Tests.SqliteRepositoryTests();
    await repo.Full_Sqlite_Test_Create_Insert_Select_Delete();
}

 
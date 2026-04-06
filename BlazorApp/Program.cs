using System.Security.Authentication;
using BlazorApp.App.Health;
using BlazorApp.App.Models;
using BlazorApp.Components;
using BlazorApp.Repositories.Implementations;
using BlazorApp.Repositories.Interfaces;
using BlazorApp.Services.Implementations;
using BlazorApp.Services.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<AzureBlobOptions>(
    builder.Configuration.GetSection("AzureBlobStorage"));

builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();  // Add memory cache for reducing Cosmos hits
builder.Services.AddSingleton<IFormDefinitionService, FormDefinitionService>();
builder.Services.AddSingleton<IFormValidationService, FormValidationService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
// These two are better as Scoped (per-request)
builder.Services.AddScoped<IFormSubmissionService, FormSubmissionService>();

// === COSMOS DB REGISTRATION (updated) ===
var cosmosConnectionString = builder.Configuration["Cosmos:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("Cosmos");

if (string.IsNullOrWhiteSpace(cosmosConnectionString))
{
    throw new InvalidOperationException(
        "Cosmos DB connection string is missing! " +
        "Add it to appsettings.json or User Secrets under 'Cosmos:ConnectionString'");
}

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connString = config.GetValue<string>("Cosmos:ConnectionString")
        ?? throw new InvalidOperationException("Cosmos connection string missing");

    return new CosmosClient(connString);
});

// Add MongoDB configuration
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Register MongoDB client & database as singletons (recommended)
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;

    var mongoSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);

    // ── This is the important line ──
    mongoSettings.SslSettings = new SslSettings
    {
        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,   // or just Tls12 if Tls13 causes issues
        CheckCertificateRevocation = false,
    };

    // Optional but often helpful with Atlas
    mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(45);
    mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(45);
    mongoSettings.SocketTimeout = TimeSpan.FromSeconds(120);

    return new MongoClient(mongoSettings);
});

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var mongoSettings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var database = client.GetDatabase(mongoSettings.DatabaseName);
    return database;
});

// Register the base repository
// For Azure Cosmos DB
/*builder.Services.AddSingleton<IFormSubmissionRepository>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var baseRepository = new FormSubmissionCosmosRepository(cosmosClient);

    // Wrap with caching decorator for better performance
    var memoryCache = sp.GetRequiredService<IMemoryCache>();
    return new CachedFormSubmissionRepository(baseRepository, memoryCache);
});*/
// For Mongo DB
builder.Services.AddScoped<IFormSubmissionRepository, FormSubmissionMongoRepository>();

builder.Services.AddScoped<MongoPingHandler>();

var app = builder.Build();

// Warm-up Cosmos DB SDK and repository initialization on startup so the first user request
// doesn't bear the SDK connection / gateway / JIT cost. This blocks startup briefly but
// avoids a long delay (5-10s) on the first page navigation that needs the repository.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var cosmos = scope.ServiceProvider.GetRequiredService<CosmosClient>();
        // Warm-up the SDK connection
        cosmos.GetDatabase("dynamicsdb").GetContainer("dynamics_submissions").ReadContainerAsync().GetAwaiter().GetResult();

        // Also eagerly initialize the repository to populate the partition key path
        // This ensures the first query doesn't pay the overhead
        var repository = scope.ServiceProvider.GetRequiredService<IFormSubmissionRepository>();
        _ = repository.GetPagedAsync(null, 5, null).GetAwaiter().GetResult();
    }
    catch
    {
        // Ignore warm-up failures here; real calls will surface errors as usual.
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapMongoHealthEndpoints();

app.Run();

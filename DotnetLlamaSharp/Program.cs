using Dotnet.Chroma.Repositories.Extensions;
using Dotnet.LangSearch.SDK.Extensions;
using Dotnet.OllamaSharp.LameChain.SDK.Extensions;
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Utilities.Telemetry;
using DotnetLlamaSharp.Extensions;
using DotnetLlamaSharp.Infrastructure.Services.LlmTools;
using DotnetLlamaSharp.Mappers;
using Microsoft.Extensions.DependencyModel;
using Microsoft.OpenApi;
using Serilog;
using System.Reflection;

Log.Logger = new LoggerConfiguration()
    .WriteTo
    .Console()
    .CreateLogger();

Log.Information("App init ...");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    // Add services to the container.

    builder.Services.AddControllers();

    builder.Services
        .AddConfigurations(builder.Configuration)
        .AddChromaConfiguration(builder.Configuration)
        .AddDefaultChromaRepository()
        .AddChromaDbClient(builder.Configuration)
        .ConfigureGroqSettings(builder.Configuration)
        .AddGroqApiClient(builder.Configuration)
        .ConfigureClaudeApiClient(builder.Configuration)
        .ConfigureLameChain(builder.Configuration, ServiceLifetime.Scoped)
        .WithPythonExecutor(builder.Configuration, ServiceLifetime.Scoped)
        .WithToolsFrom<ChromaTools>(ServiceLifetime.Scoped)
        .ConfigureLangSearch(builder.Configuration)
        .AddPdfDocumentLoader()
        .AddMarkdownDocumentLoader()
        .AddSkillsLoader()
        .AddLangfuseTracing(builder.Configuration)
        .AddAutoMapper(cfg => {
            cfg.AddMaps(new[] {
                typeof(DocumentsMappingProfile),
                typeof(PromptsMappingProfile),
                typeof(EmbeddingsMappingProfile),
                typeof(LameChainMappingProfile)
            });
        })
        .AddServicesFromAssemblies(DependencyContext.Default.RuntimeLibraries
            .SelectMany(lib => lib.GetDefaultAssemblyNames(DependencyContext.Default)
                .Where(x => x.Name.Contains(Assembly.GetEntryAssembly().GetName().Name))
            )
            .ToList())
        .AddCorsPolicy()
        .AddSwaggerGen(cfg => {
            cfg.SwaggerDoc("LameSamplesController", new OpenApiInfo { Title = "Lame Samples", Version = "v1" });
            cfg.SwaggerDoc("LangSearchController", new OpenApiInfo { Title = "Lang Search", Version = "v1" });
            cfg.SwaggerDoc("PromptingController", new OpenApiInfo { Title = "Prompting", Version = "v1" });
            cfg.SwaggerDoc("ChromaController", new OpenApiInfo { Title = "Chroma", Version = "v1" });
            cfg.SwaggerDoc("FilesController", new OpenApiInfo { Title = "Files", Version = "v1" });
            cfg.SwaggerDoc("ApiManagementController", new OpenApiInfo { Title = "ApiManagement", Version = "v1" });
            cfg.SwaggerDoc("EmbeddingsController", new OpenApiInfo { Title = "Embeddings", Version = "v1" });
        });
    
    var app = builder.Build();

    app.UseSwagger()
       .UseSwaggerUI(cfg => {
           cfg.SwaggerEndpoint("/swagger/ChromaController/swagger.json", "Chroma");
           cfg.SwaggerEndpoint("/swagger/PromptingController/swagger.json", "Prompting");
           cfg.SwaggerEndpoint("/swagger/LameSamplesController/swagger.json", "Lame Samples");
           cfg.SwaggerEndpoint("/swagger/LangSearchController/swagger.json", "Lang Search");
           cfg.SwaggerEndpoint("/swagger/FilesController/swagger.json", "Files");
           cfg.SwaggerEndpoint("/swagger/ApiManagementController/swagger.json", "ApiManagement");
           cfg.SwaggerEndpoint("/swagger/EmbeddingsController/swagger.json", "Embeddings");
       });
    
    app.UseHttpsRedirection()
       .Configure();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, $"App terminated unexpectedly >> REASON: {ex.Message}");
}
finally
{
    if(LameTelemetry.HasSource)
        LameTelemetry.StopSource();

    Log.Information("Shut down complete...");
    Log.CloseAndFlush();
}

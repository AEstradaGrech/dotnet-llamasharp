using Dotnet.Chroma.Repositories.Extensions;
using Dotnet.LangSearch.SDK.Extensions;
using Dotnet.OllamaSharp.LameChain.SDK.Extensions;
using DotnetLlamaSharp.Extensions;
using DotnetLlamaSharp.Infrastructure.Services.LlmTools;
using DotnetLlamaSharp.Mappers;
using Microsoft.Extensions.DependencyModel;
using Microsoft.OpenApi;
using Serilog;
using System.Reflection;

/*
 * [!!!] https://langsearch.com/overview
 SEMKER: https://github.com/elbruno/semantickernel-localLLMs/blob/main/src/sk-ollamacsharp/OllamaChatCompletionService.cs

OLLAMASHARP: https://awaescher.github.io/OllamaSharp/docs/getting-started.html
    -- OllamaSharp vs. Microsoft.Extensions.AI vs. Semantic Kernel --
    
        Prefer OllamaSharp if:
            - you plan to use Ollama models only
            - you want to use the native Ollama API, not only chats and embeddings but model management, usage information and more
    
        Prefer Microsoft.Extensions.AI if: 
            - you only need chat and embedding functionality
            - you want to be able to use different providers like Ollama, OpenAI, Hugging Face, etc.
    
        Prefer Semantic Kernel if:
            - you need the highest flexibility with different providers, plugins, middlewares, caching, memory and more
            - you need advanced prompt techniques like variable substitution and templating
            - you want to build agentic systems
 */

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
        .ConfigureGroqSettings(builder.Configuration)
        .AddGroqApiClient(builder.Configuration)
        .ConfigureClaudeApiClient(builder.Configuration)
        .ConfigureLameChain(builder.Configuration, ServiceLifetime.Scoped)
        .WithToolsFrom<LlamaSharpTools>(ServiceLifetime.Scoped)
        .ConfigureLangSearch(builder.Configuration)
        .AddChromaConfiguration(builder.Configuration)
        .AddDefaultChromaRepository()
        .AddChromaClient(builder.Configuration)
        .AddPdfDocumentLoader()
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
            cfg.SwaggerDoc("ApiManagementController", new OpenApiInfo { Title = "ApiManagement", Version = "v1" });
            cfg.SwaggerDoc("EmbeddingsController", new OpenApiInfo { Title = "Embeddings", Version = "v1" });
        });
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    // Configure the HTTP request pipeline.

    app.UseSwagger()
       .UseSwaggerUI(cfg => {
           cfg.SwaggerEndpoint("/swagger/ChromaController/swagger.json", "Chroma");
           cfg.SwaggerEndpoint("/swagger/PromptingController/swagger.json", "Prompting");
           cfg.SwaggerEndpoint("/swagger/LameSamplesController/swagger.json", "Lame Samples");
           cfg.SwaggerEndpoint("/swagger/LangSearchController/swagger.json", "Lang Search");
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
    Log.Fatal(ex, "App terminated unexpectedly");
}
finally
{
    Log.Information("Shut down complete...");
    Log.CloseAndFlush();
}

using Dotnet.Chroma.Repositories.Extensions;
using DotnetLlamaSharp.Extensions;
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
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    //builder.Services.AddOpenApi();

    builder.Services
        .AddConfigurations(builder.Configuration)
        .AddOllamaSharpApiClient(builder.Configuration)
        .AddOllamaEmbeddingsGenerator(builder.Configuration)
        .AddLangSearchClient(builder.Configuration)
        .AddChromaConfiguration(builder.Configuration)
        .AddDefaultChromaRepository()
        .AddChromaClient(builder.Configuration)
        .AddPdfDocumentLoader()
        .AddWordDocumentLoader()
        .AddAutoMapper(cfg => {
            cfg.AddMaps(new[] {
                typeof(DocumentsMappingProfile),
                typeof(PromptsMappingProfile),
                typeof(EmbeddingsMappingProfile)
            });
        })
        .AddServicesFromAssemblies(DependencyContext.Default.RuntimeLibraries
            .SelectMany(lib => lib.GetDefaultAssemblyNames(DependencyContext.Default)
                .Where(x => x.Name.Contains(Assembly.GetEntryAssembly().GetName().Name))
            )
            .ToList())
        .AddCorsPolicy()
        //.AddEndpointsApiExplorer()
        .AddSwaggerGen(cfg => {
            cfg.SwaggerDoc("ApiManagement", new OpenApiInfo { Title = "ApiManagement", Version = "v1" });
            cfg.SwaggerDoc("Prompting", new OpenApiInfo { Title = "Prompting", Version = "v1" });
        });
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    // Configure the HTTP request pipeline.

    app.UseSwagger()
       .UseSwaggerUI(cfg => {
        cfg.SwaggerEndpoint("/swagger/ApiManagement/swagger.json", "ApiManagement");
        cfg.SwaggerEndpoint("/swagger/Prompting/swagger.json", "Prompting");
       });
    

    //app.UseSerilogRequestLogging();

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

using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders;
using DotnetLlamaSharp.Infrastructure.Settings;
using DotnetLlamaSharp.Models.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.Chroma;
using OllamaSharp;
using OllamaSharp.Models;
using System.Net;
using System.Reflection;
using static OllamaSharp.OllamaApiClient;

namespace DotnetLlamaSharp.Extensions
{
    public static class StartupConfiguration
    {
        public static IApplicationBuilder Configure(this IApplicationBuilder app)
            => app.ConfigureGlobalErrorHandler()
                  .UseCors();

        public static IServiceCollection AddServicesFromAssemblies(this IServiceCollection services, List<AssemblyName> assemblyNames, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            var assemblies = new List<Assembly>();

            foreach (var name in assemblyNames)
                assemblies.Add(Assembly.Load(name.Name));

            return services.AddServicesFromAssemblies(assemblies, lifetime);
        }
        public static IServiceCollection AddServicesFromAssemblies(this IServiceCollection services, List<Assembly> assemblies, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            => lifetime switch {
                ServiceLifetime.Scoped => services.Scan(source => source
                    .FromAssemblies(assemblies)
                    .AddClasses()
                    .AsMatchingInterface()
                    .WithScopedLifetime()),
                ServiceLifetime.Transient => services.Scan(source => source
                    .FromAssemblies(assemblies)
                    .AddClasses()
                    .AsMatchingInterface()
                    .WithTransientLifetime()),
                _ => services
            };
        
        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RequestOptions>(configuration.GetSection("OllamaSettings"));
            services.Configure<OllamaApiSettings>(configuration.GetSection(nameof(OllamaApiSettings)));
            services.Configure<ChromaDbSettings>(configuration.GetSection(nameof(ChromaDbSettings)));

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
           => services.AddCors(options => {
               options.AddPolicy("CorsPolicy",
                   builder => builder
                   .SetIsOriginAllowed((host) => true)
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials());
           });
        public static IServiceCollection AddPdfDocumentLoader(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            => lifetime switch {
                ServiceLifetime.Scoped => services.AddScoped<IDocumentLoader<PdfLoaderService>, PdfLoaderService>(),
                ServiceLifetime.Transient => services.AddTransient<IDocumentLoader<PdfLoaderService>, PdfLoaderService>(),
                _ => services
            };
        public static IServiceCollection AddWordDocumentLoader(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
           => lifetime switch
           {
               ServiceLifetime.Scoped => services.AddScoped<IDocumentLoader<WordLoaderService>, WordLoaderService>(),
               ServiceLifetime.Transient => services.AddTransient<IDocumentLoader<WordLoaderService>, WordLoaderService>(),
               _ => services
           };
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public static IServiceCollection AddChromaClient(this IServiceCollection services, IConfiguration configuration, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            => lifetime switch {
                ServiceLifetime.Transient => services.AddTransient<IChromaClient, ChromaClient>(cfg => new ChromaClient(configuration.GetSection(nameof(ChromaDbSettings)).GetValue<string>(nameof(ChromaDbSettings.ServerUrl)))),
                ServiceLifetime.Scoped => services.AddScoped<IChromaClient, ChromaClient>(cfg => new ChromaClient(configuration.GetSection(nameof(ChromaDbSettings)).GetValue<string>(nameof(ChromaDbSettings.ServerUrl)))),
                _ => services
            };
        private static IApplicationBuilder ConfigureGlobalErrorHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        await context.Response.WriteAsync(new ApiError(
                            context.Response.StatusCode, 
                            $"Internal Server Error. Exception Message :: {contextFeature.Error.Message}")
                            .ToString());
                    }
                });
            });

            return app;
        }

        // Default Ollamasharp registration.
        public static IServiceCollection AddOllamaSharpApiClient(this IServiceCollection services, IConfiguration appConfig)
        {
            var ollamaCfg = appConfig.GetSection(nameof(OllamaApiSettings));
            //TODO: GetAppsettingsConfig 4 llama
            services.AddScoped<IOllamaApiClient, OllamaApiClient>(cfg =>
            {
                var settings = new Configuration
                {
                    Uri = new Uri(ollamaCfg.GetValue<string>(nameof(OllamaApiSettings.ServerUrl))),
                    Model = ollamaCfg.GetValue<string>(nameof(OllamaApiSettings.DefaultModel))
                };
                return new OllamaApiClient(settings);
            });

            return services;
        }

        public static IServiceCollection AddOllamaEmbeddingsGenerator(this IServiceCollection services, IConfiguration appConfig)
        {
            var ollamaCfg = appConfig.GetSection(nameof(OllamaApiSettings));
            //TODO: GetAppsettingsConfig 4 llama
            services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>, OllamaApiClient>(cfg => {
                var settings = new Configuration
                {
                    Uri = new Uri(ollamaCfg.GetValue<string>(nameof(OllamaApiSettings.ServerUrl))),
                    Model = ollamaCfg.GetValue<string>(nameof(OllamaApiSettings.DefaultEmbedder))
                };
                return new OllamaApiClient(settings);
            });
            return services;
        }
        // IChatClient is for Microsoft.Extensions.AI, IOllamaApiClient is for OllamaSharp, you can register both if you want to use them side by side
        public static IServiceCollection AddOllamaIChatClient(this IServiceCollection services, IConfiguration appConfig)
        {
            var ollamaCfg = appConfig.GetSection(nameof(OllamaApiSettings));

            services.AddScoped<IChatClient>(cfg =>
            {
                var settings = new Configuration
                {
                    Uri = new Uri(ollamaCfg.GetValue<string>(nameof(OllamaApiSettings.ServerUrl))),
                    Model = ollamaCfg.GetValue<string>(nameof(OllamaApiSettings.DefaultModel))
                };
                return new OllamaApiClient(settings);
            });
            return services;
        }

        //TODO: KernelImpl -> IChatCompletionService is for Semantic Kernel, you can register it side by side with the other two if you want to use them together
    }
}

using DotnetLlamaSharp.Domain.Services.DocumentLoader;
using DotnetLlamaSharp.Infrastructure.Services.DocumentLoaders;
using DotnetLlamaSharp.Infrastructure.Settings;
using DotnetLlamaSharp.Models.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OllamaSharp.Models;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Options;
using static OllamaSharp.OllamaApiClient;
using DotnetLlamaSharp.Infrastructure.Exceptions;


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
            services.Configure<ApiSettings>(configuration.GetSection(nameof(ApiSettings)));
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
                        if (contextFeature.Error is HttpException)
                            context.Response.StatusCode = (int)((HttpException)contextFeature.Error).StatusCode;

                        await context.Response.WriteAsync(new ApiError(
                            context.Response.StatusCode, 
                            $"Application Error :: {contextFeature.Error.GetType().Name} >> {contextFeature.Error.Message}")
                            .ToString());
                    }
                });
            });

            return app;
        }

        // Default Ollamasharp registration.
        public static IServiceCollection AddOllamaSharpApiClient(this IServiceCollection services, IConfiguration appConfig)
            => services.AddScoped<IOllamaApiClient, OllamaApiClient>(sp => {
                var config = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
                var settings = new Configuration
                {
                    Uri = new Uri(config.EndpointByKey("ollama")),
                    Model = config.DefaultModel
                };
                return new OllamaApiClient(settings);
            });

        public static IServiceCollection AddOllamaEmbeddingsGenerator(this IServiceCollection services, IConfiguration appConfig)
            => services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>, OllamaApiClient>(sp => {
                    var config = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
                    var settings = new Configuration
                    {
                        Uri = new Uri(config.EndpointByKey("ollama")),
                        Model = config.DefaultEmbedder
                    };
                    return new OllamaApiClient(settings);
                });

        
        // IChatClient is for Microsoft.Extensions.AI, IOllamaApiClient is for OllamaSharp, you can register both if you want to use them side by side
        public static IServiceCollection AddOllamaIChatClient(this IServiceCollection services, IConfiguration appConfig)
            => services.AddScoped<IChatClient>(sp => {
                var config = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
                var settings = new Configuration
                {
                    Uri = new Uri(config.EndpointByKey("ollama")),
                    Model = config.DefaultModel
                };
                return new OllamaApiClient(settings);
            });


        //TODO: KernelImpl -> IChatCompletionService is for Semantic Kernel, you can register it side by side with the other two if you want to use them together
    }
}

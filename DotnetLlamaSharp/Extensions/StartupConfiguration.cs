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
using Dotnet.OllamaSharp.LameChain.SDK.Infrastructure.Interfaces;
using DotnetLlamaSharp.Infrastructure.Services.LlmTools;
using Dotnet.OllamaSharp.LameChain.SDK.Extensions;


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

        public static IServiceCollection AddToolServices(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            => services.WithToolsFrom<LlamaSharpTools>(lifetime);
        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
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
        


        //TODO: KernelImpl -> IChatCompletionService is for Semantic Kernel, you can register it side by side with the other two if you want to use them together
    }
}

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using WhisperAPI.Services.Context;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.NLPAPI;
using WhisperAPI.Services.Questions;
using WhisperAPI.Services.Search;
using WhisperAPI.Services.Suggestions;
using WhisperAPI.Settings;

namespace WhisperAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    });
            });
            services.AddMvc();

            services.AddSwaggerGen(c =>
                c.SwaggerDoc("v2", new Info { Title = "WhisperAPI", Version = "v2" }));

            var applicationSettings = new ApplicationSettings();
            this.Configuration.Bind(applicationSettings);

            ConfigureDependency(services, applicationSettings);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            loggerFactory.AddLog4Net();

            app.UseStaticFiles();
            app.UseCors("AllowAll");
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v2/swagger.json", "WhisperAPI v2"));
            app.UseMvc();
        }

        private static void ConfigureDependency(IServiceCollection services, ApplicationSettings applicationSettings)
        {
            services.AddTransient<ISuggestionsService>(
                x => new SuggestionsService(
                    x.GetService<IIndexSearch>(),
                    x.GetService<IDocumentFacets>(),
                    x.GetService<IFilterDocuments>()));

            services.AddTransient<IQuestionsService>(x => new QuestionsService());

            services.AddTransient<INlpCall>(
                x => new NlpCall(
                    x.GetService<HttpClient>(),
                    applicationSettings.IrrelevantIntents,
                    applicationSettings.NlpApiBaseAddress));

            services.AddTransient<IDocumentFacets>(
                x => new DocumentFacets(
                    x.GetService<HttpClient>(),
                    applicationSettings.MlApiBaseAddress));

            services.AddTransient<IFilterDocuments>(
                x => new FilterDocuments(
                    x.GetService<HttpClient>(),
                    applicationSettings.MlApiBaseAddress));

            services.AddTransient<IIndexSearch>(
                x => new IndexSearch(
                    applicationSettings.ApiKey,
                    applicationSettings.NumberOfResults,
                    x.GetService<HttpClient>(),
                    applicationSettings.SearchBaseAddress));

            services.AddTransient<HttpClient, HttpClient>();

            services.AddSingleton<IContexts>(
                x => new InMemoryContexts(
                    TimeSpan.Parse(applicationSettings.ContextLifeSpan)));
        }
    }
}

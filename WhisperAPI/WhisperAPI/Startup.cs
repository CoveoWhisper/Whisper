﻿using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using WhisperAPI.Services;
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

        private static void ConfigureDependency(IServiceCollection services, ApplicationSettings applicationSettings)
        {
            services.AddTransient<ISuggestionsService>(
                x => new SuggestionsService(
                    x.GetService<IIndexSearch>(),
                    x.GetService<INlpCall>(),
                    applicationSettings.IrrelevantIntents));

            services.AddTransient<INlpCall>(
                x => new NlpCall(
                    x.GetService<HttpClient>(),
                    applicationSettings.NlpApiBaseAddress));

            services.AddTransient<IIndexSearch>(
                x => new IndexSearch(
                    applicationSettings.ApiKey,
                    x.GetService<HttpClient>(),
                    applicationSettings.SearchBaseAddress));

            services.AddTransient<HttpClient, HttpClient>();

            services.AddSingleton<IContexts>(
                x => new InMemoryContexts(
                    TimeSpan.Parse(applicationSettings.ContextLifeSpan)));
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
    }
}

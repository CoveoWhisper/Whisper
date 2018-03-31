﻿using System;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using WhisperAPI.Services;

namespace WhisperAPI.Registries
{
    public class WhisperApiRegistry : StructureMap.Registry
    {
        public WhisperApiRegistry(string apiKey)
        {
            this.For<ISuggestionsService>().Use<SuggestionsService>();
            this.For<IIndexSearch>().Use<IndexSearch>().Ctor<string>("apiKey").Is(apiKey);
            this.For<HttpClient>().Use<HttpClient>();
            this.For<Contexts>().Use<Contexts>().Ctor<DbContextOptions<Contexts>>("options").Is(new DbContextOptionsBuilder<Contexts>().UseInMemoryDatabase("contextDB").Options);
        }
    }
}

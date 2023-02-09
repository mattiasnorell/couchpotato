using System.IO;
using Couchpotato.Business;
using Couchpotato.Business.Playlist;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Plugins;
using Microsoft.Extensions.Configuration;
using Couchpotato.Business.Compression;
using Couchpotato.Business.Settings;
using Couchpotato.Business.Validation;
using Couchpotato.Business.IO;
using Couchpotato.Business.Cache;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Reflection;
using System;
using couchpotato;
using Serilog;

namespace Couchpotato
{
    internal abstract class Program
    {
        private static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ??
                             throw new InvalidOperationException())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false).Build();


            var logPath = config.GetSection("logPath").Value ?? Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "log.txt");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logPath)
                .MinimumLevel.Debug()
                .CreateLogger(); 
            
            var services = new ServiceCollection()
                .AddHttpClient()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IApplication, Application>()
                .AddSingleton<ICompression, Compression>()
                .AddSingleton<IPlaylistProvider, PlaylistProvider>()
                .AddSingleton<IPlaylistParser, PlaylistParser>()
                .AddSingleton<IEpgProvider, EpgProvider>()
                .AddSingleton<IFileHandler, FileHandler>()
                .AddSingleton<IStreamValidator, StreamValidator>()
                .AddSingleton<IPluginHandler, PluginHandler>()
                .AddSingleton<ILogging, Logging>()
                .AddSingleton<ICacheProvider, CacheProvider>()
                .AddSingleton<IPlaylistItemMapper, PlaylistItemMapper>()
                .AddTransient<IHttpClientWrapper, HttpClientWrapper>((serviceProvider) =>
                {
                    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                    return new HttpClientWrapper(httpClientFactory.CreateClient());
                })
                .AddScoped<ISettingsProvider, SettingsProvider>()
                .BuildServiceProvider();


            var app = services.GetService<IApplication>();
            app.Run(args);
        }
    }
}
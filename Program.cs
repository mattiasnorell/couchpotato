using System.IO;
using Autofac;
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

namespace Couchpotato
{
    class Program {
        static void Main(string[] args) {

            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();
            
            var builder = new ContainerBuilder();
            builder.Register(context => config).As<IConfiguration>();
            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterType<Compression>().As<ICompression>();
            builder.RegisterType<PlaylistProvider>().As<IPlaylistProvider>();
            builder.RegisterType<PlaylistParser>().As<IPlaylistParser>();
            builder.RegisterType<EpgProvider>().As<IEpgProvider>();
            builder.RegisterType<FileHandler>().As<IFileHandler>();
            builder.RegisterType<SettingsProvider>().As<ISettingsProvider>();
            builder.RegisterType<StreamValidator>().As<IStreamValidator>();
            builder.RegisterType<PluginHandler>().As<IPluginHandler>();
            builder.RegisterType<Logging>().As<ILogging>();
            builder.RegisterType<CacheProvider>().As<ICacheProvider>();
           
            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var app = scope.Resolve<IApplication>();
                app.Run(args);
            }
        }
    }
}

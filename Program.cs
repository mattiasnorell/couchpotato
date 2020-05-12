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
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Couchpotato
{

    class Program
    {
        static void Main(string[] args)
        {

            var services = new ServiceCollection();
            services.AddHttpClient();
            
            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            var builder = new ContainerBuilder();
            builder.RegisterType<HttpClientWrapper>().As<IHttpClientWrapper>().WithParameter(
                (p, ctx) => p.ParameterType == typeof(HttpClient),
                (p, ctx) => ctx.Resolve<IHttpClientFactory>().CreateClient());

            builder.Register(context => config.Build()).As<IConfiguration>();
            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterType<Compression>().As<ICompression>();
            builder.RegisterType<PlaylistProvider>().As<IPlaylistProvider>();
            builder.RegisterType<PlaylistParser>().As<IPlaylistParser>();
            builder.RegisterType<EpgProvider>().As<IEpgProvider>();
            builder.RegisterType<FileHandler>().As<IFileHandler>();
            builder.RegisterType<StreamValidator>().As<IStreamValidator>();
            builder.RegisterType<PluginHandler>().As<IPluginHandler>();
            builder.RegisterType<Logging>().As<ILogging>();
            builder.RegisterType<CacheProvider>().As<ICacheProvider>();
            builder.RegisterType<PlaylistItemMapper>().As<IPlaylistItemMapper>();
            builder.RegisterType<SettingsProvider>().As<ISettingsProvider>().InstancePerLifetimeScope();

            builder.Populate(services);

            using(var container = builder.Build()){
                var app = container.Resolve<IApplication>();
                app.Run(container, args);
            };
        }
    }
}

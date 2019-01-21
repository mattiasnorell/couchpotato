using System.IO;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Autofac.Core;
using Couchpotato.Business;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Plugins;
using Microsoft.Extensions.Configuration;

namespace Couchpotato{
    class Program {
        private static IConfiguration Configuration { get; set; }
        static void Main(string[] args) {

            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
            
            var builder = new ContainerBuilder();
            builder.Register(context => config).As<IConfiguration>();
            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterType<Compression>().As<ICompression>();
            builder.RegisterType<ChannelProvider>().As<IChannelProvider>();
            builder.RegisterType<EpgProvider>().As<IEpgProvider>();
            builder.RegisterType<FileHandler>().As<IFileHandler>();
            builder.RegisterType<SettingsProvider>().As<ISettingsProvider>();
            builder.RegisterType<StreamValidator>().As<IStreamValidator>();
            builder.RegisterType<PluginHandler>().As<IPluginHandler>();
            builder.RegisterType<Logging>().As<ILogging>();
           
            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var app = scope.Resolve<IApplication>();
                app.Run(args);
            }
        }
    }
}

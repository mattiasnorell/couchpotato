using Autofac;

namespace Couchpotato
{
    class Program
    {
        static void Main(string[] args)
        {

            var builder = new ContainerBuilder();
            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterType<Compression>().As<ICompression>();
            builder.RegisterType<ChannelProvider>().As<IChannelProvider>();
            builder.RegisterType<EpgProvider>().As<IEpgProvider>();
            builder.RegisterType<FileHandler>().As<IFileHandler>();
            builder.RegisterType<SettingsProvider>().As<ISettingsProvider>();
            
            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var app = scope.Resolve<IApplication>();
                app.Run(args[0]);
            }
        }
    }
}

using System;
using CouchpotatoShared.Plugins;

namespace Couchpotato.Plugins
{
    [CouchpotatoPlugin(PluginType.ApplicationStart)]
    public class HelloWorldPlugin : IPlugin
    {
        public void Run()
        {
            Console.WriteLine("HelloWorld plug-in running!");
        }
    }
}
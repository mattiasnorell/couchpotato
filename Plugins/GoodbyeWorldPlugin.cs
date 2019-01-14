using System;

namespace Couchpotato.Plugins{
    [CouchpotatoPlugin(PluginType.ApplicationFinished)]
    public class GoodbyeWorldPlugin : IPlugin
    {
        public void Run()
        {
            Console.WriteLine("GoodbyeWorld plug-in running!");
        }
    }
}
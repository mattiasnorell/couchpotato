using System;

namespace Couchpotato.Plugins{
    public class HelloWorldPlugin : IPlugin
    {
        public void Run()
        {
            Console.WriteLine("HelloWorld plug-in running!");
        }
    }
}
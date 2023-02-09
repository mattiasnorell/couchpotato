using System;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Couchpotato.Business.Logging
{
    public class Logging : ILogging
    {
        private readonly IConfiguration _configuration;

        public Logging(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void PrintSameLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"\r{message}");
        }

        public void Print(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }

        public void Error(string message, Exception exception = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);

            if (exception != null)
            {
                Log.Error(exception, message);
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Progress(string message, int index, int max)
        {
            var verboseLogging = _configuration.GetSection($"consoleOutput")?.Value;
            if (string.IsNullOrEmpty(verboseLogging) || verboseLogging != "verbose")
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.White;
            PrintSameLine($"{message}: {((decimal)index / (decimal)max):0%}");

            if (index >= max)
            {
                Console.WriteLine("\n");
            }
        }
    }
}
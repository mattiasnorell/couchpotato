using System;

namespace Couchpotato.Business.Logging {
    public class Logging : ILogging
    {
        public Logging(){

        }
        
        public void PrintSameLine(string message){
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"\r{message}");
        }

        public void Print(string message){
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }

        public void Error(string message, Exception exception = null){
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            
            if(exception != null){
                Console.WriteLine($"Exception: \n{exception}");
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

        public void Progress(string message, int index, int max){
            Console.ForegroundColor = ConsoleColor.White;
            PrintSameLine($"{message}: {((decimal)index / (decimal)max).ToString("0%")}");

            if(index >= max){
                Console.WriteLine("\n");
            }
        }
    }
}
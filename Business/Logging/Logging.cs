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
        }

        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);    
        }

        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);    
        }
    }
}
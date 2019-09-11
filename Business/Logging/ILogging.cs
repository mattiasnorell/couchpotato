using System;

namespace Couchpotato.Business.Logging{
    public interface ILogging
    {
        void Print(string message);
        void PrintSameLine(string message);
        void Error(string message, Exception exception = null);
        void Info(string message);
        void Warn(string message);
        void Progress(string message, int index, int max);
    }
}
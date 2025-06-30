using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace UdonRabbit.Analyzer.Utils
{
    public static class UdonRabbitLogger
    {
        [Conditional("DEBUG")]
        public static void Log(string message, [CallerFilePath] string filePath = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            File.AppendAllLines(@"C:\GitHub\VRChat\VRChatBuildyThing\UdonRabbit.log", new[]
            {
                $"[{DateTime.Now}] [{Path.GetFileName(filePath)}:{lineNumber}#{memberName}] {message}",
            });
        }
    }
}
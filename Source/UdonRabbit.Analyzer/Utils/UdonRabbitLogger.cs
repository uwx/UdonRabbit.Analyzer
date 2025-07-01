using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UdonRabbit.Analyzer.Utils
{
    public static class UdonRabbitLogger
    {
        public static bool IsTestingEnvironment { get; set; }
        private static readonly object LockObj = new();
        
        [Conditional("DEBUG")]
        public static void Log(string message, [CallerFilePath] string filePath = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                lock (LockObj)
                {
                    File.AppendAllLines(@"C:\GitHub\VRChat\VRChatBuildyThing\UdonRabbit.log", new[]
                    {
                        $"[{DateTime.Now}] [{Path.GetFileName(filePath)}:{lineNumber}#{memberName}] {message}",
                    });
                }
            }
            catch (IOException ex) when (ex.Message.Contains("because it is being used by another process"))
            {
                // ignore
            }
        }

        public static Action<SyntaxNodeAnalysisContext> Catching(Action<SyntaxNodeAnalysisContext> action, [CallerFilePath] string filePath = null)
        {
            if (IsTestingEnvironment)
            {
                return action;
            }
            
            return context =>
            {
                try
                {
                    action(context);
                }
                catch (Exception e)
                {
                    Log($"While processing {Path.GetFileName(filePath)}: {e}");
                    throw;
                }
            };
        }
    }
}
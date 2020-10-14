using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SwagfinAnalytics
{
    public static class AnalyticsLogger
    {
        public static void LogInformation(string log)
        {
            Console.WriteLine($"{Environment.NewLine} {log} At {DateTime.Now:yyyy-MM-dd hh:mm:ss}");
        }

        public static void LogWarning(string log)
        {
            Console.WriteLine($"WARNING: {Environment.NewLine} {log} At {DateTime.Now:yyyy-MM-dd hh:mm:ss}");
        }
        public static void LogError(string log)
        {
            Console.WriteLine($"ERROR: {Environment.NewLine} {log} At {DateTime.Now:yyyy-MM-dd hh:mm:ss}");
        }

        public static void ClearLogs()
        {
            //Do Nothing for Now
        }
    }
}

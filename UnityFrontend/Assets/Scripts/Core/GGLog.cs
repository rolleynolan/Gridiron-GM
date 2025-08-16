using System;
using System.IO;
using UnityEngine;

namespace Gridiron.Core
{
    public static class GGLog
    {
        private static string LogFile
        {
            get { return GGPaths.Save("gg_log.txt"); }
        }

        public static void Info(string message)
        {
            Debug.Log(message);
            Append("INFO", message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(message);
            Append("WARN", message);
        }

        public static void Error(string message)
        {
            Debug.LogError(message);
            Append("ERROR", message);
        }

        private static void Append(string level, string message)
        {
#if !UNITY_EDITOR
            try
            {
                File.AppendAllText(LogFile, string.Format("{0}: {1}{2}", level, message, Environment.NewLine));
            }
            catch
            {
            }
#endif
        }
    }
}

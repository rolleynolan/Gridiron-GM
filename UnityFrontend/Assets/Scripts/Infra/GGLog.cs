namespace Gridiron.Infra
{
    internal static class GGLog
    {
        public static void Info(string message) => Gridiron.Core.GGLog.Info(message);
        public static void Warn(string message) => Gridiron.Core.GGLog.Warn(message);
        public static void Error(string message) => Gridiron.Core.GGLog.Error(message);
    }
}

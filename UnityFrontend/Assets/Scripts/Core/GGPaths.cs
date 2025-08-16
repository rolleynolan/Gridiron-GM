using System.IO;
using UnityEngine;

namespace Gridiron.Core
{
    public static class GGPaths
    {
        public static string Streaming(string file)
        {
            return Path.Combine(Application.streamingAssetsPath, file);
        }

        public static string Save(string file)
        {
            return Path.Combine(Application.persistentDataPath, file);
        }

        public static string Project(string relative)
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(root, relative));
        }
    }
}

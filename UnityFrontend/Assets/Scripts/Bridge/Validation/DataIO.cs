using System.IO;
using UnityEngine;
using Gridiron.Core;
using Gridiron.Bridge.Dto;

namespace Gridiron.Bridge.Validation
{
    public static class DataIO
    {
        public static T LoadJson<T>(string relativePath)
        {
            var fullPath = GGPaths.Project(relativePath.TrimStart('/'));
            GGLog.Info("Loading " + fullPath);
            var json = File.ReadAllText(fullPath);
            if (typeof(T) == typeof(ContractDTO))
            {
                object dto = ContractDTO.FromJson(json);
                return (T)dto;
            }
            return JsonUtility.FromJson<T>(json);
        }
    }
}

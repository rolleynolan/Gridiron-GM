#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Gridiron.Core;

public static class PrefabTools
{
    [MenuItem("GG/Tools/Clear Season Save")]
    public static void ClearSeasonSave()
    {
        var path = GGPaths.Save(GGConventions.SeasonSaveFile);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Season save cleared");
        }
    }

    [MenuItem("GG/Tools/Scan Missing Scripts")]
    public static void ScanMissingScripts()
    {
        var objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in objects)
        {
            var comps = go.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null)
                {
                    Debug.LogWarning("Missing script on " + go.name);
                }
            }
        }
    }
}
#endif

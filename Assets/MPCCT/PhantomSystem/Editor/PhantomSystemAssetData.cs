using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MPCCT
{
    public class PhantomSystemAssetData : ScriptableObject
    {
        [Serializable]
        private class Entry
        {
            public string key;
            public string guid;
        }

        [SerializeField]
        private List<Entry> _entries = new List<Entry>();

        private static PhantomSystemAssetData Instance;

        private static List<Entry> entries
        {
            get
            {
                if (Instance == null)
                {
                    var guids = AssetDatabase.FindAssets("t:PhantomSystemAssetData");
                    if (guids.Length == 0)
                    {
                        Instance = CreateInstance<PhantomSystemAssetData>();
                        Debug.LogError("[PhantomSystem] PhantomSystemAssetData asset not found. Please reinstall PhantomSystem");
                    }
                    else
                    {
                        Instance = AssetDatabase.LoadAssetAtPath<PhantomSystemAssetData>(AssetDatabase.GUIDToAssetPath(guids[0]));
                        if (guids.Length > 1) Debug.LogWarning("[PhantomSystem] Multiple PhantomSystemAssetData assets found. Delete PhantomSystem folder and reinstall it if anything is wrong.");
                    }
                        
                }
                return Instance._entries;
            }
        }

        internal static string GetGuid(string key)
        {
            var e = entries.FirstOrDefault(x => x.key == key);
            return e?.guid;
        }

        internal static string ResolvePath(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;

            var guid = GetGuid(key);
            if (!string.IsNullOrEmpty(guid))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                return p;
            }
            return null;
        }
    }
}
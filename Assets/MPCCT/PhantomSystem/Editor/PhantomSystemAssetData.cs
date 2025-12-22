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
        private List<Entry> entries = new List<Entry>();

        private const string DeafaultAssetGUID = "2b0356b3670b7e74eb7e4fef59729cce";

        public static PhantomSystemAssetData Load()
        {
            var db = AssetDatabase.LoadAssetAtPath<PhantomSystemAssetData>(AssetDatabase.GUIDToAssetPath(DeafaultAssetGUID));
            return db;
        }

        internal string GetGuid(string key)
        {
            var e = entries.FirstOrDefault(x => x.key == key);
            return e?.guid;
        }

        internal static List<string> GetAllKeys()
        {
            var db = Load();
            return db.entries.Select(x => x.key).ToList();
        }

        internal static string ResolvePath(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;

            var db = Load();
            var guid = db.GetGuid(key);
            if (!string.IsNullOrEmpty(guid))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                return p;
            }
            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MPCCT
{
    public class PhantomSystemLocalizationData : ScriptableObject
    {
        [Serializable]
        private class Entry
        {
            public string key;
            [TextArea] public string en;
            [TextArea] public string zh;
            [TextArea] public string jp;
        }

        internal enum Locale { English = 0, Chinese = 1, Japanese = 2 }
        internal static Locale currentLocale = Locale.English;

        [SerializeField]
        private List<Entry> _entries = new List<Entry>();

        private static PhantomSystemLocalizationData Instance;

        private static List<Entry> entries
        {
            get
            {
                if (Instance == null)
                {
                    var guids = AssetDatabase.FindAssets("t:PhantomSystemLocalizationData");
                    if (guids.Length == 0)
                    {
                        Debug.LogError("[PhantomSystem] PhantomSystemLocalizationData asset not found.  Please reinstall PhantomSystem");
                        Instance = CreateInstance<PhantomSystemLocalizationData>();
                    }
                    else
                    {
                        Instance = AssetDatabase.LoadAssetAtPath<PhantomSystemLocalizationData>(AssetDatabase.GUIDToAssetPath(guids[0]));
                        if (guids.Length > 1) Debug.LogWarning("[PhantomSystem] Multiple PhantomSystemLocalizationData assets found. Delete PhantomSystem folder and reinstall it if anything is wrong.");
                    }
                }
                return Instance._entries;
            }
        }

        internal static string Text(string key)
        {
            var e = entries.FirstOrDefault(x => x.key == key);
            if (e == null) return null;
            switch (currentLocale)
            {
                case Locale.Chinese: return e.zh;
                case Locale.Japanese: return e.jp;
                default: return e.en;
            }
        }
    }
}
using UnityEditor;
using UnityEngine;

namespace MPCCT
{
    [CustomEditor(typeof(PhantomSystemAssetData))]
    public class AssetGuidDatabaseEditor : Editor
    {
        private SerializedProperty entriesProp;

        private void OnEnable()
        {
            entriesProp = serializedObject.FindProperty("_entries");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (entriesProp == null)
            {
                DrawDefaultInspector();
                return;
            }

            EditorGUILayout.LabelField("PhantomSystem Asset Database", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var e = entriesProp.GetArrayElementAtIndex(i);
                var keyProp = e.FindPropertyRelative("key");
                var guidProp = e.FindPropertyRelative("guid");

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(keyProp.stringValue, GUILayout.ExpandWidth(true));

                var currentObj = string.IsNullOrEmpty(guidProp.stringValue) ? null : AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guidProp.stringValue));
                var newObj = EditorGUILayout.ObjectField(currentObj, typeof(Object), false);
                if (newObj != currentObj)
                {
                    if (newObj == null)
                    {
                        guidProp.stringValue = "";
                    }
                    else
                    {
                        var path = AssetDatabase.GetAssetPath(newObj);
                        guidProp.stringValue = AssetDatabase.AssetPathToGUID(path);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
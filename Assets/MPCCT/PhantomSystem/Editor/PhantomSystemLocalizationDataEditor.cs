using UnityEditor;
using UnityEngine;

namespace MPCCT
{
    [CustomEditor(typeof(PhantomSystemLocalizationData))]
    internal class PhantomSystemLocalizationDataEditor : Editor
    {
        private SerializedProperty entriesProp;
        private bool[] foldouts;

        private void OnEnable()
        {
            entriesProp = serializedObject.FindProperty("_entries");
            foldouts = new bool[entriesProp.arraySize];
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("PhantomSystem Localization Data", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var elem = entriesProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical();

                foldouts[i] = EditorGUILayout.Foldout(foldouts[i], elem.FindPropertyRelative("key").stringValue, true);

                if (foldouts[i])
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("en"), new GUIContent("en"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("zh"), new GUIContent("zh"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("jp"), new GUIContent("jp"));
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(1);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
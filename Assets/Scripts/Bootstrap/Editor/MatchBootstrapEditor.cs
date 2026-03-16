#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Bomber.Bootstrap
{
    [CustomEditor(typeof(MatchBootstrap))]
    public sealed class MatchBootstrapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "MatchBootstrap owns the generated arena preview. " +
                "Main Camera remains the authoring camera: move and rotate Main Camera directly in the scene, " +
                "then use the button below if you want a one-click arena framing pass.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Arena", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("arenaWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("arenaHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("arenaCellSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("arenaRandomSeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("arenaCrateFill"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Match", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enemyCount"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Camera", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraFollowLerp"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoFrameMainCamera"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraEulerAngles"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraFieldOfView"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraFramePadding"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("syncSceneViewToPreview"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            MatchBootstrap bootstrap = (MatchBootstrap)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Preview"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(bootstrap, "Rebuild Match Preview");
                    bootstrap.RebuildPreviewNow();
                    EditorUtility.SetDirty(bootstrap);
                }

                if (GUILayout.Button("Frame Main Camera"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(bootstrap, "Frame Main Camera");
                    bootstrap.FrameMainCameraNow();
                    EditorUtility.SetDirty(bootstrap);
                }
            }

            if (GUILayout.Button("Align Scene View"))
            {
                serializedObject.ApplyModifiedProperties();
                bootstrap.AlignSceneViewToMainCameraNow();
                EditorUtility.SetDirty(bootstrap);
            }

            if (GUILayout.Button("Clear Generated Roots"))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(bootstrap, "Clear Generated Roots");
                bootstrap.ClearGeneratedNow();
                EditorUtility.SetDirty(bootstrap);
            }

            if (GUILayout.Button("Select Main Camera"))
            {
                bootstrap.SelectMainCameraNow();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bomber.Bootstrap
{
    [InitializeOnLoad]
    public static class MatchBootstrapEditorBootstrap
    {
        static MatchBootstrapEditorBootstrap()
        {
            EditorApplication.delayCall += EnsureBootstrapForDefaultScene;
            EditorSceneManager.newSceneCreated += HandleNewSceneCreated;
        }

        private static void HandleNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            EditorApplication.delayCall += EnsureBootstrapForDefaultScene;
        }

        private static void EnsureBootstrapForDefaultScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return;
            }

            if (Object.FindObjectOfType<MatchBootstrap>() != null)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            if (!LooksLikeDefaultAuthoringScene(roots))
            {
                return;
            }

            GameObject bootstrap = new GameObject("MatchBootstrap");
            bootstrap.AddComponent<MatchBootstrap>();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool LooksLikeDefaultAuthoringScene(GameObject[] roots)
        {
            if (roots.Length == 0)
            {
                return true;
            }

            if (roots.Length > 2)
            {
                return false;
            }

            for (int i = 0; i < roots.Length; i++)
            {
                string rootName = roots[i].name;
                if (rootName != "Main Camera" && rootName != "Directional Light")
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif

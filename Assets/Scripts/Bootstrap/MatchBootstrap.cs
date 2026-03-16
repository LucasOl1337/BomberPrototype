using Bomber.Gameplay;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bomber.Bootstrap
{
    [ExecuteAlways]
    public sealed class MatchBootstrap : MonoBehaviour
    {
        private const string PreviewRootName = "__MatchPreview";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindObjectOfType<MatchBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrap = new GameObject("MatchBootstrap");
            bootstrap.AddComponent<MatchBootstrap>();
        }

        private void Awake()
        {
            ClearPreviewRoot();

            if (!Application.isPlaying)
            {
                return;
            }

            BuildMatch();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            SchedulePreviewRebuild();
        }

        private void OnValidate()
        {
            SchedulePreviewRebuild();
        }

        private void SchedulePreviewRebuild()
        {
            if (Application.isPlaying || !isActiveAndEnabled || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.delayCall -= RebuildPreviewIfNeeded;
            EditorApplication.delayCall += RebuildPreviewIfNeeded;
        }

        private void RebuildPreviewIfNeeded()
        {
            if (this == null || Application.isPlaying || !isActiveAndEnabled || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            BuildPreview();
        }
#endif

        private void BuildMatch()
        {
            ArenaGrid arena = new GameObject("Arena").AddComponent<ArenaGrid>();
            arena.Generate();

            CreateLighting();
            PlayerController player = CreatePlayer(arena);
            MatchController matchController = CreateMatchController(arena, player);
            CreateEnemies(arena, matchController);
            CreateCamera(player.transform);
            CreateHud(matchController);
        }

        private void BuildPreview()
        {
            ClearPreviewRoot();

            Transform previewRoot = new GameObject(PreviewRootName).transform;
            previewRoot.SetParent(transform, false);

            ArenaGrid arena = new GameObject("Arena").AddComponent<ArenaGrid>();
            arena.transform.SetParent(previewRoot, false);
            arena.Generate();

            CreateLighting();
            Transform player = CreatePreviewPlayer(arena, previewRoot);
            CreatePreviewEnemies(arena, previewRoot);
            ConfigurePreviewCamera(player);
        }

        private static PlayerController CreatePlayer(ArenaGrid arena)
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "Player";
            playerObject.transform.position = arena.GetPlayerSpawnWorld();

            Renderer renderer = playerObject.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0.2f, 0.55f, 1f);

            PlayerController player = playerObject.AddComponent<PlayerController>();
            player.Initialize(arena);
            playerObject.AddComponent<PlayerPixelLabVisual>();
            return player;
        }

        private static Transform CreatePreviewPlayer(ArenaGrid arena, Transform parent)
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "PlayerPreview";
            playerObject.transform.SetParent(parent, false);
            playerObject.transform.position = arena.GetPlayerSpawnWorld();

            Renderer renderer = playerObject.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial.color = new Color(0.2f, 0.55f, 1f);
            playerObject.AddComponent<PlayerPixelLabVisual>();
            return playerObject.transform;
        }

        private static void CreateEnemies(ArenaGrid arena, MatchController matchController)
        {
            foreach (Vector2Int cell in arena.GetEnemySpawnCells(3))
            {
                GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObject.name = $"Enemy_{cell.x}_{cell.y}";

                Renderer renderer = enemyObject.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0.93f, 0.2f, 0.2f);

                EnemyWalker enemy = enemyObject.AddComponent<EnemyWalker>();
                enemy.Initialize(arena, cell);
                matchController.RegisterEnemy(enemy);
            }
        }

        private static void CreatePreviewEnemies(ArenaGrid arena, Transform parent)
        {
            foreach (Vector2Int cell in arena.GetEnemySpawnCells(3))
            {
                GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObject.name = $"EnemyPreview_{cell.x}_{cell.y}";
                enemyObject.transform.SetParent(parent, false);
                enemyObject.transform.position = arena.CellToWorld(cell) + Vector3.up * 0.8f;

                Renderer renderer = enemyObject.GetComponent<Renderer>();
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(0.93f, 0.2f, 0.2f);
            }
        }

        private static MatchController CreateMatchController(ArenaGrid arena, PlayerController player)
        {
            GameObject controllerObject = new GameObject("MatchController");
            MatchController matchController = controllerObject.AddComponent<MatchController>();
            matchController.Initialize(arena, player);
            return matchController;
        }

        private static void CreateCamera(Transform target)
        {
            Camera existingCamera = Camera.main;
            Camera camera = existingCamera != null ? existingCamera : new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.08f);
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;

            TopDownCameraFollow follow = camera.gameObject.GetComponent<TopDownCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<TopDownCameraFollow>();
            }

            follow.Initialize(target);
            camera.transform.position = target.position + new Vector3(0f, 18f, -11f);
        }

        private static void ConfigurePreviewCamera(Transform target)
        {
            Camera existingCamera = Camera.main;
            Camera camera = existingCamera != null ? existingCamera : new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.08f);
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = target.position + new Vector3(0f, 18f, -11f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        }

        private static void CreateLighting()
        {
            if (FindObjectOfType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.42f);
        }

        private static void CreateHud(MatchController matchController)
        {
            GameObject hudObject = new GameObject("MatchHud");
            MatchHud hud = hudObject.AddComponent<MatchHud>();
            hud.Initialize(matchController);
        }

        private void ClearPreviewRoot()
        {
            Transform previewRoot = transform.Find(PreviewRootName);
            if (previewRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(previewRoot.gameObject);
            }
            else
            {
                DestroyImmediate(previewRoot.gameObject);
            }
        }
    }
}

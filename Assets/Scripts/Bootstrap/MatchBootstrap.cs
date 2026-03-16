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
        private const string GeneratedRootName = "__GeneratedMatch";
        private const string LegacyPreviewRootName = "__MatchPreview";
        private const string LegacyRuntimeRootName = "__MatchRuntime";

        [Header("Arena")]
        [SerializeField] private int arenaWidth = 13;
        [SerializeField] private int arenaHeight = 11;
        [SerializeField] private float arenaCellSize = 2f;
        [SerializeField] private int arenaRandomSeed = 1337;
        [SerializeField, Range(0f, 1f)] private float arenaCrateFill = 0.72f;

        [Header("Match")]
        [SerializeField] private int enemyCount = 3;

        [Header("Runtime Camera")]
        [SerializeField] private float cameraFollowLerp = 8f;
        [SerializeField] private bool autoFrameMainCamera = true;
        [SerializeField] private Vector3 cameraEulerAngles = new Vector3(58f, 0f, 0f);
        [SerializeField] private float cameraFieldOfView = 45f;
        [SerializeField] private float cameraFramePadding = 1.15f;
        [SerializeField] private bool syncSceneViewToPreview = true;

#if UNITY_EDITOR
        private bool previewQueued;
#endif

        private bool isRebuilding;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                RebuildRuntimeNow();
            }
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            QueuePreviewRebuild();
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= RebuildPreviewIfNeeded;
            previewQueued = false;
        }

        private void OnValidate()
        {
            QueuePreviewRebuild();
        }

        private void QueuePreviewRebuild()
        {
            if (Application.isPlaying || !isActiveAndEnabled || EditorApplication.isPlayingOrWillChangePlaymode || previewQueued)
            {
                return;
            }

            previewQueued = true;
            EditorApplication.delayCall += RebuildPreviewIfNeeded;
        }

        private void RebuildPreviewIfNeeded()
        {
            EditorApplication.delayCall -= RebuildPreviewIfNeeded;
            previewQueued = false;

            if (this == null || Application.isPlaying || !isActiveAndEnabled || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            BuildWorld(includeRuntimeSystems: false);
        }

        public void RebuildPreviewNow()
        {
            if (Application.isPlaying)
            {
                return;
            }

            BuildWorld(includeRuntimeSystems: false);
        }

        public void FrameMainCameraNow()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Camera camera = GetOrCreateMainCamera(out _);
            ApplyDefaultCameraPresentation(camera);
            FrameCameraToArena(camera, forceConfiguredRotation: true);
            AlignSceneViewToMainCameraNow();
        }

        public void SelectMainCameraNow()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                Selection.activeObject = camera.gameObject;
            }
        }

        public void AlignSceneViewToMainCameraNow()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Camera camera = Camera.main;
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (camera == null || sceneView == null)
            {
                return;
            }

            sceneView.orthographic = false;
            sceneView.LookAtDirect(GetArenaCenter(), camera.transform.rotation, GetSceneViewSize());
            sceneView.Repaint();
        }
#endif

        public void RebuildRuntimeNow()
        {
            BuildWorld(includeRuntimeSystems: true);
        }

        public void ClearGeneratedNow()
        {
            ClearGeneratedHierarchy();
            ClearLegacySceneArtifacts();
            RemoveCameraFollowInEditMode();
        }

        private void BuildWorld(bool includeRuntimeSystems)
        {
            if (isRebuilding)
            {
                return;
            }

            isRebuilding = true;
            try
            {
                EnsureSingleBootstrapInstance();
                ClearGeneratedHierarchy();
                ClearLegacySceneArtifacts();

                Transform generatedRoot = new GameObject(GeneratedRootName).transform;
                generatedRoot.SetParent(transform, false);

                ArenaGrid arena = CreateArena(generatedRoot);
                EnsureLighting();

                Camera camera = GetOrCreateMainCamera(out bool createdCamera);
                ApplyDefaultCameraPresentation(camera);

                if (includeRuntimeSystems)
                {
                    PlayerController player = CreatePlayer(arena, generatedRoot);
                    MatchController matchController = CreateMatchController(arena, player, generatedRoot);
                    CreateEnemies(arena, matchController, generatedRoot, enemyCount);
                    ConfigureRuntimeCamera(camera, player.transform);
                    CreateHud(matchController, generatedRoot);
                }
                else
                {
                    CreatePreviewPlayer(arena, generatedRoot);
                    CreatePreviewEnemies(arena, generatedRoot, enemyCount);
                    RemoveCameraFollowInEditMode();

                    if (createdCamera || autoFrameMainCamera)
                    {
                        FrameCameraToArena(camera, forceConfiguredRotation: autoFrameMainCamera);
                    }

#if UNITY_EDITOR
                    if (syncSceneViewToPreview)
                    {
                        AlignSceneViewToMainCameraNow();
                    }
#endif
                }
            }
            finally
            {
                isRebuilding = false;
            }
        }

        private ArenaGrid CreateArena(Transform parent)
        {
            ArenaGrid arena = new GameObject("Arena").AddComponent<ArenaGrid>();
            arena.transform.SetParent(parent, false);
            arena.Configure(arenaWidth, arenaHeight, arenaCellSize, arenaRandomSeed, arenaCrateFill);
            arena.Generate();
            return arena;
        }

        private static PlayerController CreatePlayer(ArenaGrid arena, Transform parent)
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "Player";
            playerObject.transform.SetParent(parent, false);
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

        private static void CreateEnemies(ArenaGrid arena, MatchController matchController, Transform parent, int count)
        {
            foreach (Vector2Int cell in arena.GetEnemySpawnCells(count))
            {
                GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObject.name = $"Enemy_{cell.x}_{cell.y}";
                enemyObject.transform.SetParent(parent, false);

                Renderer renderer = enemyObject.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0.93f, 0.2f, 0.2f);

                EnemyWalker enemy = enemyObject.AddComponent<EnemyWalker>();
                enemy.Initialize(arena, cell);
                matchController.RegisterEnemy(enemy);
            }
        }

        private static void CreatePreviewEnemies(ArenaGrid arena, Transform parent, int count)
        {
            foreach (Vector2Int cell in arena.GetEnemySpawnCells(count))
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

        private static MatchController CreateMatchController(ArenaGrid arena, PlayerController player, Transform parent)
        {
            GameObject controllerObject = new GameObject("MatchController");
            controllerObject.transform.SetParent(parent, false);
            MatchController matchController = controllerObject.AddComponent<MatchController>();
            matchController.Initialize(arena, player);
            return matchController;
        }

        private void ConfigureRuntimeCamera(Camera camera, Transform target)
        {
            TopDownCameraFollow follow = camera.gameObject.GetComponent<TopDownCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<TopDownCameraFollow>();
            }

            Vector3 authoredOffset = camera.transform.position - target.position;
            if (autoFrameMainCamera || authoredOffset.sqrMagnitude < 1f)
            {
                FrameCameraToArena(camera, forceConfiguredRotation: autoFrameMainCamera);
                authoredOffset = camera.transform.position - target.position;
            }

            follow.Initialize(target, authoredOffset, cameraFollowLerp, camera.transform.rotation.eulerAngles);
        }

        private static Camera GetOrCreateMainCamera(out bool created)
        {
            PruneDuplicateMainCameras();

            Camera camera = Camera.main;
            if (camera == null)
            {
                Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (cameras.Length > 0)
                {
                    camera = cameras[0];
                    camera.tag = "MainCamera";
                }
            }

            if (camera != null)
            {
                created = false;
                return camera;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            created = true;
            return camera;
        }

        private void FrameCameraToArena(Camera camera, bool forceConfiguredRotation)
        {
            Quaternion rotation = forceConfiguredRotation
                ? Quaternion.Euler(cameraEulerAngles)
                : camera.transform.rotation;

            if (rotation == Quaternion.identity)
            {
                rotation = Quaternion.Euler(cameraEulerAngles);
            }

            float arenaWorldWidth = arenaWidth * arenaCellSize;
            float arenaWorldHeight = arenaHeight * arenaCellSize;
            float halfWidth = arenaWorldWidth * 0.5f;
            float halfHeight = arenaWorldHeight * 0.5f;

            float minX = -halfWidth;
            float maxX = halfWidth;
            float minZ = -halfHeight;
            float maxZ = halfHeight;
            float minY = -0.7f;
            float maxY = 2.2f;

            Vector3 center = new Vector3(0f, (minY + maxY) * 0.5f, 0f);
            Vector3[] corners =
            {
                new Vector3(minX, minY, minZ),
                new Vector3(minX, minY, maxZ),
                new Vector3(minX, maxY, minZ),
                new Vector3(minX, maxY, maxZ),
                new Vector3(maxX, minY, minZ),
                new Vector3(maxX, minY, maxZ),
                new Vector3(maxX, maxY, minZ),
                new Vector3(maxX, maxY, maxZ)
            };

            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;

            float aspect = camera.aspect > 0.01f ? camera.aspect : (16f / 9f);
            float verticalHalfFov = Mathf.Max(0.01f, camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float horizontalHalfFov = Mathf.Atan(Mathf.Tan(verticalHalfFov) * aspect);

            float requiredDistance = 1f;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 relative = corners[i] - center;
                float x = Mathf.Abs(Vector3.Dot(relative, right));
                float y = Mathf.Abs(Vector3.Dot(relative, up));
                float z = Vector3.Dot(relative, forward);

                requiredDistance = Mathf.Max(requiredDistance, (x / Mathf.Tan(horizontalHalfFov)) - z);
                requiredDistance = Mathf.Max(requiredDistance, (y / Mathf.Tan(verticalHalfFov)) - z);
            }

            requiredDistance *= Mathf.Max(1.01f, cameraFramePadding);

            camera.transform.SetPositionAndRotation(center - (forward * requiredDistance), rotation);
        }

        private Vector3 GetArenaCenter()
        {
            return new Vector3(0f, 0.75f, 0f);
        }

        private float GetSceneViewSize()
        {
            float arenaWorldWidth = arenaWidth * arenaCellSize;
            float arenaWorldHeight = arenaHeight * arenaCellSize;
            float largestDimension = Mathf.Max(arenaWorldWidth, arenaWorldHeight);
            return largestDimension * Mathf.Max(0.6f, cameraFramePadding);
        }

        private void EnsureSingleBootstrapInstance()
        {
            MatchBootstrap[] bootstraps = Object.FindObjectsByType<MatchBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bootstraps.Length; i++)
            {
                MatchBootstrap other = bootstraps[i];
                if (other == null || other == this || other.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                if (IsDisposableDuplicateBootstrap(other))
                {
                    DestroyObject(other.gameObject);
                }
            }
        }

        private static bool IsDisposableDuplicateBootstrap(MatchBootstrap bootstrap)
        {
            if (bootstrap.transform.parent != null)
            {
                return false;
            }

            if (bootstrap.transform.childCount == 0)
            {
                return true;
            }

            for (int i = 0; i < bootstrap.transform.childCount; i++)
            {
                string childName = bootstrap.transform.GetChild(i).name;
                if (childName != GeneratedRootName && childName != LegacyPreviewRootName && childName != LegacyRuntimeRootName)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyDefaultCameraPresentation(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.08f);
            camera.fieldOfView = Mathf.Clamp(cameraFieldOfView, 30f, 70f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
        }

        private static void EnsureLighting()
        {
            if (Object.FindAnyObjectByType<Light>() != null)
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

        private static void CreateHud(MatchController matchController, Transform parent)
        {
            GameObject hudObject = new GameObject("MatchHud");
            hudObject.transform.SetParent(parent, false);
            MatchHud hud = hudObject.AddComponent<MatchHud>();
            hud.Initialize(matchController);
        }

        private void ClearGeneratedHierarchy()
        {
            ClearChildRoot(GeneratedRootName);
            ClearChildRoot(LegacyPreviewRootName);
            ClearChildRoot(LegacyRuntimeRootName);
        }

        private void ClearChildRoot(string rootName)
        {
            Transform child = transform.Find(rootName);
            if (child == null)
            {
                return;
            }

            DestroyObject(child.gameObject);
        }

        private void ClearLegacySceneArtifacts()
        {
            if (!gameObject.scene.IsValid())
            {
                return;
            }

            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || root == gameObject)
                {
                    continue;
                }

                if (IsLegacyGeneratedRoot(root))
                {
                    DestroyObject(root);
                }
            }
        }

        private static bool IsLegacyGeneratedRoot(GameObject root)
        {
            string name = root.name;
            if (name == "Arena" || name == "Player" || name == "PlayerPreview" || name == "MatchController" || name == "MatchHud")
            {
                return true;
            }

            if (name.StartsWith("Enemy_") || name.StartsWith("EnemyPreview_"))
            {
                return true;
            }

            if (name == "Sun" && root.GetComponent<Light>() != null)
            {
                return true;
            }

            return false;
        }

        private static void PruneDuplicateMainCameras()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Camera keep = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate == null || candidate.gameObject.scene.name == null)
                {
                    continue;
                }

                if (candidate.CompareTag("MainCamera"))
                {
                    keep = candidate;
                    break;
                }
            }

            if (keep == null && cameras.Length > 0)
            {
                keep = cameras[0];
                keep.tag = "MainCamera";
            }

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate == null || candidate == keep)
                {
                    continue;
                }

                if (IsDisposableDuplicateMainCamera(candidate))
                {
                    DestroyObject(candidate.gameObject);
                }
            }
        }

        private static bool IsDisposableDuplicateMainCamera(Camera camera)
        {
            if (camera.gameObject.name != "Main Camera" || camera.transform.parent != null)
            {
                return false;
            }

            Component[] components = camera.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component is Transform || component is Camera || component is AudioListener || component is TopDownCameraFollow)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static void RemoveCameraFollowInEditMode()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            TopDownCameraFollow follow = camera.GetComponent<TopDownCameraFollow>();
            if (follow == null)
            {
                return;
            }

            Object.DestroyImmediate(follow);
        }

        private static void DestroyObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}

using Bomber.Gameplay;
using UnityEngine;

namespace Bomber.Bootstrap
{
    public sealed class MatchBootstrap : MonoBehaviour
    {
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
            BuildMatch();
        }

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
            return player;
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
    }
}

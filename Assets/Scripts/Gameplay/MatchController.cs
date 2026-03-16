using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bomber.Gameplay
{
    public sealed class MatchController : MonoBehaviour
    {
        private const float CratePickupDropChance = 0.35f;
        private const float WallPickupDropChance = 0.5f;

        private ArenaGrid arena;
        private PlayerController player;
        private readonly List<EnemyWalker> enemies = new List<EnemyWalker>();
        private bool matchWon;
        private bool matchLost;
        private int cratesDestroyed;
        private int enemiesDefeated;

        public PlayerController Player => player;
        public int RemainingEnemies => enemies.Count;
        public int CratesDestroyed => cratesDestroyed;
        public int EnemiesDefeated => enemiesDefeated;
        public bool MatchWon => matchWon;
        public bool MatchLost => matchLost;
        public bool MatchFinished => matchWon || matchLost;

        public void Initialize(ArenaGrid arenaGrid, PlayerController playerController)
        {
            arena = arenaGrid;
            player = playerController;

            arena.CrateDestroyed += HandleCrateDestroyed;
            arena.WallDestroyed += HandleWallDestroyed;
            player.LivesChanged += HandleLivesChanged;
        }

        public void RegisterEnemy(EnemyWalker enemy)
        {
            enemies.Add(enemy);
            enemy.DestroyedEnemy += HandleEnemyDestroyed;
        }

        private void Update()
        {
            if (MatchFinished && Input.GetKeyDown(KeyCode.R))
            {
                RestartMatch();
            }
        }

        private void HandleCrateDestroyed(Vector2Int cell, Vector3 worldPosition)
        {
            cratesDestroyed++;

            if (Random.value > CratePickupDropChance)
            {
                return;
            }

            SpawnPickup(worldPosition);
        }

        private void HandleWallDestroyed(Vector2Int cell, Vector3 worldPosition)
        {
            if (Random.value > WallPickupDropChance)
            {
                return;
            }

            SpawnPickup(worldPosition);
        }

        private void HandleEnemyDestroyed(EnemyWalker enemy)
        {
            enemy.DestroyedEnemy -= HandleEnemyDestroyed;
            enemies.Remove(enemy);
            enemiesDefeated++;

            if (enemies.Count == 0 && !matchLost)
            {
                matchWon = true;
                SetActorControls(false);
            }
        }

        private void HandleLivesChanged(int currentLives, int maxLives)
        {
            if (currentLives > 0 || matchWon)
            {
                return;
            }

            matchLost = true;
            SetActorControls(false);
        }

        private void SetActorControls(bool enabled)
        {
            if (player != null)
            {
                player.SetControlsEnabled(enabled);
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].SetMovementEnabled(enabled);
                }
            }
        }

        private void SpawnPickup(Vector3 position)
        {
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pickupObject.name = "Pickup";
            pickupObject.transform.position = new Vector3(position.x, 0.45f, position.z);
            pickupObject.transform.localScale = new Vector3(0.7f, 0.2f, 0.7f);

            PowerUpPickup pickup = pickupObject.AddComponent<PowerUpPickup>();
            pickup.Initialize((PowerUpPickup.PowerUpType)Random.Range(0, 3));
        }

        private void RestartMatch()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            if (!string.IsNullOrEmpty(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }

        private void OnDestroy()
        {
            if (arena != null)
            {
                arena.CrateDestroyed -= HandleCrateDestroyed;
                arena.WallDestroyed -= HandleWallDestroyed;
            }

            if (player != null)
            {
                player.LivesChanged -= HandleLivesChanged;
            }
        }
    }
}

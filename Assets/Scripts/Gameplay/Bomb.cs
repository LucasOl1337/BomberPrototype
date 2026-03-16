using System.Collections;
using UnityEngine;

namespace Bomber.Gameplay
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Bomb : MonoBehaviour
    {
        public enum BombType
        {
            Standard,
            WallBreaker
        }

        private ArenaGrid arena;
        private Vector2Int cell;
        private PlayerController owner;
        private Collider ownerCollider;
        private SphereCollider bombCollider;
        private float fuseSeconds;
        private int explosionRange;
        private BombType bombType;
        private bool detonated;
        private bool collisionRestored;

        public void Initialize(
            ArenaGrid arenaGrid,
            Vector2Int bombCell,
            PlayerController bombOwner,
            Collider bombOwnerCollider,
            float fuseTime,
            int range,
            BombType type)
        {
            arena = arenaGrid;
            cell = bombCell;
            owner = bombOwner;
            ownerCollider = bombOwnerCollider;
            fuseSeconds = fuseTime;
            explosionRange = range;
            bombType = type;

            bombCollider = GetComponent<SphereCollider>();
            bombCollider.radius = 0.5f;

            arena.RegisterBomb(cell);
            Physics.IgnoreCollision(ownerCollider, bombCollider, true);

            StartCoroutine(FuseRoutine());
        }

        private void Update()
        {
            if (collisionRestored || ownerCollider == null || bombCollider == null)
            {
                return;
            }

            if (!ownerCollider.bounds.Intersects(bombCollider.bounds))
            {
                Physics.IgnoreCollision(ownerCollider, bombCollider, false);
                collisionRestored = true;
            }
        }

        public void DetonateNow()
        {
            if (!detonated)
            {
                Detonate();
            }
        }

        private IEnumerator FuseRoutine()
        {
            float elapsed = 0f;
            Vector3 baseScale = transform.localScale;

            while (elapsed < fuseSeconds)
            {
                elapsed += Time.deltaTime;
                float pulse = 1f + (Mathf.Sin(elapsed * 12f) * 0.08f);
                transform.localScale = baseScale * pulse;
                yield return null;
            }

            Detonate();
        }

        private void Detonate()
        {
            if (detonated)
            {
                return;
            }

            detonated = true;
            arena.UnregisterBomb(cell);
            if (owner != null)
            {
                owner.NotifyBombFinished();
            }

            SpawnExplosionAt(cell);

            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (Vector2Int direction in directions)
            {
                for (int step = 1; step <= explosionRange; step++)
                {
                    Vector2Int targetCell = cell + (direction * step);
                    if (!arena.IsInside(targetCell))
                    {
                        break;
                    }

                    if (arena.IsWall(targetCell))
                    {
                        if (bombType == BombType.WallBreaker && arena.DestroyWall(targetCell))
                        {
                            SpawnExplosionAt(targetCell);
                        }

                        break;
                    }

                    SpawnExplosionAt(targetCell);

                    if (arena.DestroyCrate(targetCell))
                    {
                        break;
                    }
                }
            }

            Destroy(gameObject);
        }

        private void SpawnExplosionAt(Vector2Int targetCell)
        {
            GameObject segmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segmentObject.name = $"Explosion_{targetCell.x}_{targetCell.y}";
            segmentObject.transform.position = arena.CellToWorld(targetCell) + Vector3.up * 0.4f;
            segmentObject.transform.localScale = new Vector3(arena.CellSize * 0.9f, 0.5f, arena.CellSize * 0.9f);

            Renderer renderer = segmentObject.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = bombType == BombType.WallBreaker
                ? new Color(0.4f, 0.95f, 1f)
                : new Color(1f, 0.45f, 0.1f);

            BoxCollider hitbox = segmentObject.GetComponent<BoxCollider>();
            hitbox.isTrigger = true;

            ExplosionSegment segment = segmentObject.AddComponent<ExplosionSegment>();
            segment.Initialize();
        }
    }
}

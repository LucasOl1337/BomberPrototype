using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bomber.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class EnemyWalker : MonoBehaviour, IDamageable
    {
        public delegate void EnemyDestroyedHandler(EnemyWalker enemy);

        [SerializeField] private float moveDuration = 0.22f;
        [SerializeField] private float pauseDuration = 0.08f;

        private ArenaGrid arena;
        private Rigidbody body;
        private Vector2Int currentCell;
        private bool isAlive = true;
        private bool movementEnabled = true;

        public event EnemyDestroyedHandler DestroyedEnemy;

        public void Initialize(ArenaGrid arenaGrid, Vector2Int startCell)
        {
            arena = arenaGrid;
            currentCell = startCell;
            transform.position = arena.CellToWorld(startCell) + Vector3.up * 0.8f;
            StartCoroutine(WalkLoop());
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.isKinematic = true;

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            capsule.height = 1.6f;
            capsule.radius = 0.42f;
            capsule.center = new Vector3(0f, 0.8f, 0f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.TryGetComponent(out PlayerController player))
            {
                player.TakeHit(gameObject);
            }
        }

        public void TakeHit(GameObject source)
        {
            if (!isAlive)
            {
                return;
            }

            isAlive = false;
            if (DestroyedEnemy != null)
            {
                DestroyedEnemy(this);
            }
            Destroy(gameObject);
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
        }

        private IEnumerator WalkLoop()
        {
            yield return new WaitForSeconds(0.5f);

            while (isAlive)
            {
                if (!movementEnabled)
                {
                    yield return null;
                    continue;
                }

                List<Vector2Int> candidates = GetOpenNeighbors();
                if (candidates.Count > 0)
                {
                    Vector2Int targetCell = candidates[Random.Range(0, candidates.Count)];
                    yield return MoveToCell(targetCell);
                    currentCell = targetCell;
                }

                yield return new WaitForSeconds(pauseDuration);
            }
        }

        private List<Vector2Int> GetOpenNeighbors()
        {
            var candidates = new List<Vector2Int>();
            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int nextCell = currentCell + direction;
                if (!arena.IsInside(nextCell) || arena.IsWall(nextCell) || arena.IsCrate(nextCell) || arena.HasBomb(nextCell))
                {
                    continue;
                }

                candidates.Add(nextCell);
            }

            return candidates;
        }

        private IEnumerator MoveToCell(Vector2Int targetCell)
        {
            Vector3 start = transform.position;
            Vector3 end = arena.CellToWorld(targetCell) + Vector3.up * 0.8f;
            Vector3 direction = (end - start).normalized;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                body.MovePosition(Vector3.Lerp(start, end, t));
                yield return null;
            }
        }
    }
}

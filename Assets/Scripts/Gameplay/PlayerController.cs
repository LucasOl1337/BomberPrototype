using UnityEngine;

namespace Bomber.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour, IDamageable
    {
        public delegate void PlayerStateChangedHandler();
        public delegate void PlayerLivesChangedHandler(int currentLives, int maxLives);

        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float turnSpeed = 16f;
        [SerializeField] private int maxBombs = 1;
        [SerializeField] private float bombFuseSeconds = 2f;
        [SerializeField] private int explosionRange = 3;
        [SerializeField] private int maxLives = 3;
        [SerializeField] private float respawnInvulnerabilitySeconds = 1.5f;

        private ArenaGrid arena;
        private Rigidbody body;
        private CapsuleCollider bodyCollider;
        private Vector3 moveInput;
        private int activeBombs;
        private Vector3 spawnPoint;
        private float planeY;
        private float invulnerableUntil;
        private int lives;
        private bool controlsEnabled = true;

        public int Lives => lives;
        public int MaxLives => maxLives;
        public int MaxBombs => maxBombs;
        public int ExplosionRange => explosionRange;
        public float MoveSpeed => moveSpeed;
        public bool IsAlive => lives > 0;

        public event PlayerStateChangedHandler StatsChanged;
        public event PlayerLivesChangedHandler LivesChanged;

        public void Initialize(ArenaGrid arenaGrid)
        {
            arena = arenaGrid;
            spawnPoint = arena.GetPlayerSpawnWorld();
            planeY = spawnPoint.y;
            lives = maxLives;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            bodyCollider = GetComponent<CapsuleCollider>();
            bodyCollider.height = 1.6f;
            bodyCollider.radius = 0.45f;
            bodyCollider.center = new Vector3(0f, 0.8f, 0f);
        }

        private void Update()
        {
            if (!controlsEnabled || !IsAlive)
            {
                moveInput = Vector3.zero;
                return;
            }

            moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryPlaceBomb(Bomb.BombType.Standard);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                TryPlaceBomb(Bomb.BombType.WallBreaker);
            }
        }

        private void FixedUpdate()
        {
            Camera mainCamera = Camera.main;
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (mainCamera != null)
            {
                forward = mainCamera.transform.forward;
                right = mainCamera.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            Vector3 desiredVelocity = controlsEnabled ? ((forward * moveInput.z) + (right * moveInput.x)).normalized * moveSpeed : Vector3.zero;
            Vector3 nextPosition = body.position + (desiredVelocity * Time.fixedDeltaTime);
            nextPosition.y = planeY;
            body.MovePosition(nextPosition);

            Vector3 lookDirection = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
            }
        }

        public void TakeHit(GameObject source)
        {
            if (Time.time < invulnerableUntil || !IsAlive)
            {
                return;
            }

            lives = Mathf.Max(0, lives - 1);
            activeBombs = 0;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = spawnPoint;
            invulnerableUntil = Time.time + respawnInvulnerabilitySeconds;
            NotifyLivesChanged();
        }

        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;
            if (!enabled)
            {
                moveInput = Vector3.zero;
            }
        }

        public void NotifyBombFinished()
        {
            activeBombs = Mathf.Max(0, activeBombs - 1);
            NotifyStatsChanged();
        }

        public void AddBombCapacity(int amount)
        {
            maxBombs += Mathf.Max(1, amount);
            NotifyStatsChanged();
        }

        public void AddExplosionRange(int amount)
        {
            explosionRange += Mathf.Max(1, amount);
            NotifyStatsChanged();
        }

        public void AddMoveSpeed(float amount)
        {
            moveSpeed += Mathf.Max(0.25f, amount);
            NotifyStatsChanged();
        }

        private void TryPlaceBomb(Bomb.BombType bombType)
        {
            if (arena == null || activeBombs >= maxBombs)
            {
                return;
            }

            Vector2Int cell = arena.WorldToCell(transform.position);
            if (!arena.CanPlaceBomb(cell))
            {
                return;
            }

            GameObject bombObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bombObject.name = $"Bomb_{cell.x}_{cell.y}";
            bombObject.transform.position = arena.CellToWorld(cell) + Vector3.up * 0.5f;
            bombObject.transform.localScale = Vector3.one * 0.9f;

            Renderer renderer = bombObject.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = bombType == Bomb.BombType.WallBreaker
                ? new Color(0.12f, 0.7f, 0.82f)
                : new Color(0.08f, 0.08f, 0.1f);

            Bomb bomb = bombObject.AddComponent<Bomb>();
            bomb.Initialize(arena, cell, this, bodyCollider, bombFuseSeconds, explosionRange, bombType);

            activeBombs++;
            NotifyStatsChanged();
        }

        private void NotifyStatsChanged()
        {
            if (StatsChanged != null)
            {
                StatsChanged();
            }
        }

        private void NotifyLivesChanged()
        {
            if (LivesChanged != null)
            {
                LivesChanged(lives, maxLives);
            }

            NotifyStatsChanged();
        }

        private void Start()
        {
            NotifyLivesChanged();
        }

        private void OnTriggerEnter(Collider other)
        {
            PowerUpPickup pickup = other.GetComponent<PowerUpPickup>();
            if (pickup != null)
            {
                pickup.Collect(this);
            }
        }
    }
}

using UnityEngine;

namespace Bomber.Gameplay
{
    public sealed class PowerUpPickup : MonoBehaviour
    {
        public enum PowerUpType
        {
            BombCapacity,
            ExplosionRange,
            MoveSpeed
        }

        [SerializeField] private PowerUpType powerUpType;
        [SerializeField] private float rotateSpeed = 120f;

        public PowerUpType Type => powerUpType;

        public void Initialize(PowerUpType type)
        {
            powerUpType = type;

            Collider trigger = GetComponent<Collider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;

            SphereCollider sphereTrigger = trigger as SphereCollider;
            if (sphereTrigger != null)
            {
                sphereTrigger.radius = 0.6f;
            }

            Renderer renderer = GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = GetColor(type);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        public void Collect(PlayerController player)
        {
            switch (powerUpType)
            {
                case PowerUpType.BombCapacity:
                    player.AddBombCapacity(1);
                    break;
                case PowerUpType.ExplosionRange:
                    player.AddExplosionRange(1);
                    break;
                case PowerUpType.MoveSpeed:
                    player.AddMoveSpeed(0.75f);
                    break;
            }

            Destroy(gameObject);
        }

        private static Color GetColor(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.BombCapacity:
                    return new Color(0.2f, 0.82f, 0.9f);
                case PowerUpType.ExplosionRange:
                    return new Color(1f, 0.66f, 0.1f);
                default:
                    return new Color(0.55f, 1f, 0.3f);
            }
        }
    }
}

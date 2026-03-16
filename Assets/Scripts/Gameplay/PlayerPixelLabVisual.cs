using System.Collections.Generic;
using UnityEngine;

namespace Bomber.Gameplay
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public sealed class PlayerPixelLabVisual : MonoBehaviour
    {
        private static readonly string[] DirectionNames =
        {
            "north",
            "north-east",
            "east",
            "south-east",
            "south",
            "south-west",
            "west",
            "north-west"
        };

        private static readonly Dictionary<string, Material> MaterialsByDirection = new Dictionary<string, Material>();

        [SerializeField] private Vector3 visualOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private Vector3 visualScale = new Vector3(1.7f, 1.9f, 1f);

        private Renderer actorRenderer;
        private Transform visualTransform;
        private MeshRenderer visualRenderer;
        private string currentDirection;
        private const string VisualObjectName = "PlayerPixelLabVisualQuad";

        private void Start()
        {
            BuildOrRefreshVisual();
        }

        private void OnEnable()
        {
            BuildOrRefreshVisual();
        }

        private void OnValidate()
        {
            BuildOrRefreshVisual();
        }

        private void BuildOrRefreshVisual()
        {
            actorRenderer = GetComponent<Renderer>();
            BuildVisual();
            UpdateDirection(force: true);
        }

        private void LateUpdate()
        {
            if (visualTransform == null || visualRenderer == null)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                visualTransform.forward = -mainCamera.transform.forward;
            }

            UpdateDirection(force: false);
        }

        private void BuildVisual()
        {
            Material southMaterial = GetMaterial("south");
            if (southMaterial == null)
            {
                return;
            }

            Transform existingVisual = transform.Find(VisualObjectName);
            if (existingVisual != null)
            {
                visualTransform = existingVisual;
                visualRenderer = existingVisual.GetComponent<MeshRenderer>();
            }

            if (actorRenderer != null)
            {
                actorRenderer.enabled = false;
            }

            if (visualTransform == null)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = VisualObjectName;
                quad.transform.SetParent(transform, false);

                Collider quadCollider = quad.GetComponent<Collider>();
                if (quadCollider != null)
                {
                    DestroyImmediate(quadCollider);
                }

                visualTransform = quad.transform;
                visualRenderer = quad.GetComponent<MeshRenderer>();
            }

            visualTransform.localPosition = visualOffset;
            visualTransform.localScale = visualScale;
            visualRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visualRenderer.receiveShadows = false;
            visualRenderer.sharedMaterial = southMaterial;
        }

        private void UpdateDirection(bool force)
        {
            string direction = GetDirectionName(transform.forward);
            if (!force && direction == currentDirection)
            {
                return;
            }

            Material material = GetMaterial(direction);
            if (material == null || visualRenderer == null)
            {
                return;
            }

            visualRenderer.sharedMaterial = material;
            currentDirection = direction;
        }

        private static string GetDirectionName(Vector3 forward)
        {
            Vector3 planarForward = new Vector3(forward.x, 0f, forward.z);
            if (planarForward.sqrMagnitude < 0.001f)
            {
                return "south";
            }

            float angle = Mathf.Atan2(planarForward.x, planarForward.z) * Mathf.Rad2Deg;
            angle = (angle + 360f + 22.5f) % 360f;
            int index = Mathf.FloorToInt(angle / 45f) % DirectionNames.Length;
            return DirectionNames[index];
        }

        private static Material GetMaterial(string direction)
        {
            if (MaterialsByDirection.TryGetValue(direction, out Material cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>("PixelLab/Player/rotations/" + direction);
            if (texture == null)
            {
                return null;
            }

            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader);
            material.mainTexture = texture;
            MaterialsByDirection[direction] = material;
            return material;
        }
    }
}

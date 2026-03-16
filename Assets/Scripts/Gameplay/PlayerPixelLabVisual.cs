using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        private bool refreshQueued;
        private const string VisualObjectName = "PlayerPixelLabVisualQuad";
        private static Mesh quadMesh;

        private void Start()
        {
            QueueRefresh();
        }

        private void OnEnable()
        {
            QueueRefresh();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= RefreshFromEditor;
#endif
            refreshQueued = false;
        }

        private void OnValidate()
        {
            QueueRefresh();
        }

        private void QueueRefresh()
        {
            if (refreshQueued)
            {
                return;
            }

            if (Application.isPlaying)
            {
                refreshQueued = true;
                BuildOrRefreshVisual();
                refreshQueued = false;
                return;
            }

#if UNITY_EDITOR
            if (!isActiveAndEnabled)
            {
                return;
            }

            refreshQueued = true;
            EditorApplication.delayCall -= RefreshFromEditor;
            EditorApplication.delayCall += RefreshFromEditor;
#endif
        }

#if UNITY_EDITOR
        private void RefreshFromEditor()
        {
            EditorApplication.delayCall -= RefreshFromEditor;
            refreshQueued = false;

            if (this == null || Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            BuildOrRefreshVisual();
        }
#endif

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
                GameObject quad = new GameObject(VisualObjectName);
                quad.name = VisualObjectName;
                quad.transform.SetParent(transform, false);

                visualTransform = quad.transform;
                MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = GetQuadMesh();
                visualRenderer = quad.AddComponent<MeshRenderer>();
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

        private static Mesh GetQuadMesh()
        {
            if (quadMesh != null)
            {
                return quadMesh;
            }

            quadMesh = new Mesh
            {
                name = "PlayerPixelLabVisualQuadMesh",
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            quadMesh.RecalculateNormals();
            return quadMesh;
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

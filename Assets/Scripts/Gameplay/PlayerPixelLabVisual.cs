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

        [Header("Asset")]
        [SerializeField] private string resourceFolder = "PixelLab/Player/rotations";

        [Header("Layout")]
        [SerializeField] private float visualHeight = 2.4f;
        [SerializeField] private float widthMultiplier = 1f;
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField] private Vector3 localOffset = Vector3.zero;

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
                actorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                actorRenderer.receiveShadows = false;
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

            ApplyVisualLayout(southMaterial.mainTexture as Texture2D);
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

            ApplyVisualLayout(material.mainTexture as Texture2D);
            visualRenderer.sharedMaterial = material;
            currentDirection = direction;
        }

        private void ApplyVisualLayout(Texture2D texture)
        {
            if (visualTransform == null)
            {
                return;
            }

            float aspect = 1f;
            if (texture != null && texture.height > 0)
            {
                aspect = (float)texture.width / texture.height;
            }

            float width = visualHeight * aspect * widthMultiplier;
            visualTransform.localScale = new Vector3(width, visualHeight, 1f);
            visualTransform.localPosition = new Vector3(
                localOffset.x,
                (visualHeight * 0.5f) + groundOffset + localOffset.y,
                localOffset.z);
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

        private Material GetMaterial(string direction)
        {
            string cacheKey = resourceFolder + "::" + direction;
            if (MaterialsByDirection.TryGetValue(cacheKey, out Material cached))
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourceFolder + "/" + direction);
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
            MaterialsByDirection[cacheKey] = material;
            return material;
        }
    }
}

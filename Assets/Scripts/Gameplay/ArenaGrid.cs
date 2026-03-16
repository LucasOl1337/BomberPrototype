using System.Collections.Generic;
using UnityEngine;

namespace Bomber.Gameplay
{
    public sealed class ArenaGrid : MonoBehaviour
    {
        public delegate void CrateDestroyedHandler(Vector2Int cell, Vector3 worldPosition);
        public delegate void WallDestroyedHandler(Vector2Int cell, Vector3 worldPosition);

        [Header("Arena")]
        [SerializeField] private int width = 13;
        [SerializeField] private int height = 11;
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private int randomSeed = 1337;
        [SerializeField, Range(0f, 1f)] private float crateFill = 0.72f;

        private readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> destructibleWalls = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> crates = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> bombs = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> openCells = new List<Vector2Int>();
        private readonly Dictionary<Vector2Int, GameObject> crateObjects = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, GameObject> wallObjects = new Dictionary<Vector2Int, GameObject>();

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector2Int PlayerSpawnCell => new Vector2Int(1, 1);

        public event CrateDestroyedHandler CrateDestroyed;
        public event WallDestroyedHandler WallDestroyed;

        public void Generate()
        {
            Random.InitState(randomSeed);

            BuildFloor();
            BuildArenaBlocks();
            CollectOpenCells();
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            float originX = -((width - 1) * cellSize) * 0.5f;
            float originZ = -((height - 1) * cellSize) * 0.5f;
            return new Vector3(originX + (cell.x * cellSize), 0f, originZ + (cell.y * cellSize));
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            float originX = -((width - 1) * cellSize) * 0.5f;
            float originZ = -((height - 1) * cellSize) * 0.5f;
            int x = Mathf.RoundToInt((worldPosition.x - originX) / cellSize);
            int y = Mathf.RoundToInt((worldPosition.z - originZ) / cellSize);
            return new Vector2Int(x, y);
        }

        public Vector3 SnapToCellCenter(Vector3 worldPosition)
        {
            return CellToWorld(WorldToCell(worldPosition));
        }

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
        }

        public bool IsWall(Vector2Int cell) => walls.Contains(cell);

        public bool IsDestructibleWall(Vector2Int cell) => destructibleWalls.Contains(cell);

        public bool IsCrate(Vector2Int cell) => crates.Contains(cell);

        public bool HasBomb(Vector2Int cell) => bombs.Contains(cell);

        public bool CanPlaceBomb(Vector2Int cell)
        {
            return IsInside(cell) && !IsWall(cell) && !IsCrate(cell) && !HasBomb(cell);
        }

        public void RegisterBomb(Vector2Int cell)
        {
            bombs.Add(cell);
        }

        public void UnregisterBomb(Vector2Int cell)
        {
            bombs.Remove(cell);
        }

        public bool DestroyCrate(Vector2Int cell)
        {
            if (!crateObjects.TryGetValue(cell, out GameObject crate))
            {
                return false;
            }

            crateObjects.Remove(cell);
            crates.Remove(cell);
            Vector3 worldPosition = crate.transform.position;
            Destroy(crate);
            if (CrateDestroyed != null)
            {
                CrateDestroyed(cell, worldPosition);
            }
            return true;
        }

        public bool DestroyWall(Vector2Int cell)
        {
            if (!destructibleWalls.Contains(cell))
            {
                return false;
            }

            GameObject wall;
            if (!wallObjects.TryGetValue(cell, out wall))
            {
                return false;
            }

            Vector3 worldPosition = wall.transform.position;
            wallObjects.Remove(cell);
            destructibleWalls.Remove(cell);
            walls.Remove(cell);
            Destroy(wall);
            CollectOpenCells();
            if (WallDestroyed != null)
            {
                WallDestroyed(cell, worldPosition);
            }
            return true;
        }

        public Vector3 GetPlayerSpawnWorld()
        {
            return CellToWorld(PlayerSpawnCell) + Vector3.up * 0.8f;
        }

        public List<Vector2Int> GetEnemySpawnCells(int count)
        {
            var valid = new List<Vector2Int>();
            foreach (Vector2Int cell in openCells)
            {
                if ((cell - PlayerSpawnCell).sqrMagnitude < 16)
                {
                    continue;
                }

                valid.Add(cell);
            }

            Shuffle(valid);
            if (valid.Count > count)
            {
                valid.RemoveRange(count, valid.Count - count);
            }

            return valid;
        }

        private void BuildFloor()
        {
            Transform root = new GameObject("Floor").transform;
            root.SetParent(transform);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 position = CellToWorld(new Vector2Int(x, y));
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Floor_{x}_{y}";
                    tile.transform.SetParent(root);
                    tile.transform.position = position + Vector3.down * 0.55f;
                    tile.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
                    Paint(tile, new Color(0.23f, 0.27f, 0.19f));
                    Object.Destroy(tile.GetComponent<Collider>());
                }
            }
        }

        private void BuildArenaBlocks()
        {
            Transform wallsRoot = new GameObject("Walls").transform;
            wallsRoot.SetParent(transform);

            Transform cratesRoot = new GameObject("Crates").transform;
            cratesRoot.SetParent(transform);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (ShouldCreateWall(cell))
                    {
                        walls.Add(cell);
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = $"Wall_{x}_{y}";
                        wall.transform.SetParent(wallsRoot);
                        wall.transform.position = CellToWorld(cell) + Vector3.up;
                        wall.transform.localScale = new Vector3(cellSize, 2f, cellSize);
                        wall.AddComponent<WallMarker>();
                        wallObjects[cell] = wall;

                        if (IsBorderWall(cell))
                        {
                            Paint(wall, new Color(0.28f, 0.32f, 0.24f));
                        }
                        else
                        {
                            destructibleWalls.Add(cell);
                            Paint(wall, new Color(0.46f, 0.5f, 0.38f));
                        }
                        continue;
                    }

                    if (ShouldCreateCrate(cell))
                    {
                        crates.Add(cell);
                        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        crate.name = $"Crate_{x}_{y}";
                        crate.transform.SetParent(cratesRoot);
                        crate.transform.position = CellToWorld(cell) + Vector3.up * 0.9f;
                        crate.transform.localScale = Vector3.one * (cellSize * 0.9f);
                        crate.AddComponent<DestructibleCrate>();
                        Paint(crate, new Color(0.62f, 0.39f, 0.18f));
                        crateObjects[cell] = crate;
                    }
                }
            }
        }

        private bool ShouldCreateWall(Vector2Int cell)
        {
            if (IsBorderWall(cell))
            {
                return true;
            }

            return cell.x % 2 == 0 && cell.y % 2 == 0;
        }

        private bool IsBorderWall(Vector2Int cell)
        {
            return cell.x == 0 || cell.y == 0 || cell.x == width - 1 || cell.y == height - 1;
        }

        private bool ShouldCreateCrate(Vector2Int cell)
        {
            if (IsReservedForSpawn(cell))
            {
                return false;
            }

            return Random.value < crateFill;
        }

        private bool IsReservedForSpawn(Vector2Int cell)
        {
            Vector2Int spawn = PlayerSpawnCell;
            return cell == spawn
                || cell == spawn + Vector2Int.up
                || cell == spawn + Vector2Int.right
                || cell == spawn + Vector2Int.right + Vector2Int.up
                || cell == new Vector2Int(width - 2, height - 2)
                || cell == new Vector2Int(width - 3, height - 2)
                || cell == new Vector2Int(width - 2, height - 3);
        }

        private void CollectOpenCells()
        {
            openCells.Clear();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (IsInside(cell) && !IsWall(cell) && !IsCrate(cell))
                    {
                        openCells.Add(cell);
                    }
                }
            }
        }

        private static void Paint(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
        }

        private static void Shuffle(List<Vector2Int> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                int randomIndex = Random.Range(i, cells.Count);
                (cells[i], cells[randomIndex]) = (cells[randomIndex], cells[i]);
            }
        }
    }
}

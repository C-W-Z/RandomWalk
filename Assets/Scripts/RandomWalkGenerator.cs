using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomWalkGenerator : MonoBehaviour
{
    class Walker
    {
        public Vector2Int Position;
        public Vector2Int Direction;
        public float ChanceToChange;
        public Walker(Vector2Int pos, Vector2Int dir, float chanceToChange){
            Position = pos;
            Direction = dir;
            ChanceToChange = chanceToChange;
        }
    }

    enum GridType { Empty, Floor, Wall }

    [SerializeField] Tilemap tileMap;
    [SerializeField] TileBase emptyTile;
    [SerializeField] TileBase floorTile;
    [SerializeField] TileBase wallTile;
    [SerializeField] int mapWidth = 30;
    [SerializeField] int mapHeight = 30;

    [SerializeField] int maxWalkerCount = 10;
    [SerializeField, Range(0f, 1f)] float chanceToChange = 0.5f;
    [SerializeField, Range(0f, 1f)] float fillPercentage = 0.4f;
    [SerializeField] float waitTime = 0.05f;
    [SerializeField] bool setEmpty = false;

    GridType[,] grid;
    List<Walker> walkers;
    int tileCount = 0;

    readonly Vector2Int[] EightDir = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new(1,1), new(1,-1), new(-1,1), new(-1,-1)
    };
    Vector2Int RandomDirection => EightDir[Random.Range(0, 4)];

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        tileCount = 0;

        grid = new GridType[mapWidth, mapHeight];

        // if (default(GridType) != GridType.Empty)
        //     for (int x = 0; x < grid.GetLength(0); x++)
        //         for (int y = 0; y < grid.GetLength(1); y++)
        //             grid[x, y] = GridType.Empty;

        Vector2Int center = new(grid.GetLength(0) / 2, grid.GetLength(1) / 2);

        walkers = new List<Walker>();
        Walker firstWalker = new(center, RandomDirection, 0.5f);

        grid[center.x, center.y] = GridType.Floor;
        tileMap.SetTile((Vector3Int)center, floorTile);
        walkers.Add(firstWalker);

        tileCount++;

        StartCoroutine(CreateFloors());
    }

    IEnumerator CreateFloors()
    {
        while ((float)tileCount / grid.Length < fillPercentage)
        {
            bool hasCreatedFloor = false;
            foreach (Walker w in walkers)
            {
                Vector3Int pos = (Vector3Int)w.Position;

                if (grid[pos.x, pos.y] != GridType.Floor)
                {
                    tileMap.SetTile(pos, floorTile);
                    tileCount++;
                    grid[pos.x, pos.y] = GridType.Floor;
                    hasCreatedFloor = true;
                }
            }

            //Walker Methods
            ChanceToRemove();
            ChanceToRedirect();
            ChanceToCreate();
            UpdatePosition();

            if (hasCreatedFloor)
                yield return new WaitForSeconds(waitTime);
        }

        StartCoroutine(CreateWalls());
    }

    void ChanceToRemove()
    {
        for (int i = 0; i < walkers.Count; i++)
            if (Random.value < walkers[i].ChanceToChange && walkers.Count > 1)
            {
                walkers.RemoveAt(i);
                break;
            }
    }

    void ChanceToRedirect()
    {
        foreach (Walker w in walkers)
            if (Random.value < w.ChanceToChange)
                w.Direction = RandomDirection;
    }

    void ChanceToCreate()
    {
        for (int i = 0; i < walkers.Count; i++)
            if (Random.value < walkers[i].ChanceToChange && walkers.Count < maxWalkerCount)
                walkers.Add(new(walkers[i].Position, RandomDirection, 0.5f));
    }

    void UpdatePosition()
    {
        foreach (Walker w in walkers)
        {
            w.Position += w.Direction;
            w.Position.x = Mathf.Clamp(w.Position.x, 1, grid.GetLength(0) - 2);
            w.Position.y = Mathf.Clamp(w.Position.y, 1, grid.GetLength(1) - 2);
        }
    }

    IEnumerator CreateWalls()
    {
        for (int x = 0; x < grid.GetLength(0) - 1; x++)
            for (int y = 0; y < grid.GetLength(1) - 1; y++)
                if (grid[x, y] == GridType.Floor)
                {
                    bool hasCreatedWall = false;

                    foreach (var d in EightDir)
                        if (grid[x + d.x, y + d.y] == GridType.Empty)
                        {
                            tileMap.SetTile(new(x + d.x, y + d.y), wallTile);
                            grid[x + d.x, y + d.y] = GridType.Wall;
                            hasCreatedWall = true;
                        }

                    if (hasCreatedWall)
                        yield return new WaitForSeconds(waitTime);
                }

        if (setEmpty)
            CreateEmpty();
    }

    void CreateEmpty()
    {
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                if (grid[x, y] == GridType.Empty)
                    tileMap.SetTile(new(x, y), emptyTile);
    }
}

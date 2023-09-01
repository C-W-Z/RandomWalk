using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomWalkGenerator : MonoBehaviour
{
    [System.Serializable] class Walker
    {
        public Vector2Int Position;
        public Vector2Int Direction;
        public Walker (Vector2Int pos, Vector2Int dir)
        {
            Position = pos;
            Direction = dir;
        }
    }

    [System.Serializable] struct WalkerChance
    {
        [Range(0f, 1f)] public float Redirect;
        [Range(0f, 1f)] public float Duplicate;
        [Range(0f, 1f)] public float Die;
    }

    enum GridType { Empty, Floor, Wall }
    [System.Serializable] struct WaitTime
    {
        public enum TimeType { Second, Frame }
        public TimeType Type;
        [Range(0f, 0.1f)] public float Second;
        [Range(0, 10)] public int Frame;
    }

    [SerializeField] Tilemap tileMap;
    [SerializeField] TileBase emptyTile;
    [SerializeField] TileBase floorTile;
    [SerializeField] TileBase wallTile;
    [SerializeField] Vector2Int mapSize = new(30, 30);
    [SerializeField] int maxWalkerCount = 10;
    [SerializeField, Range(0f, 1f)] float fillPercentage = 0.4f;
    [SerializeField] bool fillEmpty = false;
    [SerializeField] WalkerChance walkerChance;
    [SerializeField] WaitTime floorStepTime;
    [SerializeField] WaitTime wallStepTime;

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
        StartGeneration();
    }

    void StartGeneration()
    {
        tileMap.ClearAllTiles();

        tileCount = 0;

        grid = new GridType[mapSize.x, mapSize.y];

        // if (default(GridType) != GridType.Empty)
        //     for (int x = 0; x < mapSize.x; x++)
        //         for (int y = 0; y < mapSize.y; y++)
        //             grid[x, y] = GridType.Empty;

        Vector2Int center = new(mapSize.x / 2, mapSize.y / 2);
        // add first walker
        walkers = new List<Walker> { new(center, RandomDirection) };

        StartCoroutine(CreateFloors());
    }

    IEnumerator CreateFloors()
    {
        while ((float)tileCount / grid.Length < fillPercentage)
        {
            // iterate through all wakers reversely to avoid bugs from RemoveAt(i)
            bool hasCreatedFloor = false;
            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                var w = walkers[i];
                // create tiles
                if (grid[w.Position.x, w.Position.y] != GridType.Floor)
                {
                    SetTile((Vector3Int)w.Position, GridType.Floor);
                    tileCount++;
                    hasCreatedFloor = true;
                }

                // choose what to do randomly
                if (Random.value <= walkerChance.Redirect)
                    w.Direction = RandomDirection;
                if (Random.value <= walkerChance.Duplicate && walkers.Count < maxWalkerCount)
                    walkers.Add(new(w.Position, RandomDirection));
                if (Random.value <= walkerChance.Die && walkers.Count > 1)
                    walkers.RemoveAt(i);
                else
                {
                    // update position if not be removed
                    w.Position += w.Direction;
                    w.Position.x = Mathf.Clamp(w.Position.x, 1, mapSize.x - 2);
                    w.Position.y = Mathf.Clamp(w.Position.y, 1, mapSize.y - 2);
                }
            }

            if (hasCreatedFloor)
                if (floorStepTime.Type == WaitTime.TimeType.Second && floorStepTime.Second > 0)
                    yield return new WaitForSeconds(floorStepTime.Second);
                else
                    for (int i = 0; i < floorStepTime.Frame; i++)
                        yield return null;
        }

        StartCoroutine(CreateWalls());
    }

    IEnumerator CreateWalls()
    {
        for (int y = mapSize.y - 1; y >= 0; y--)
            for (int x = 0; x < mapSize.x; x++)
                if (grid[x, y] == GridType.Floor)
                {
                    bool hasCreatedWall = false;

                    foreach (var d in EightDir)
                        if (grid[x + d.x, y + d.y] == GridType.Empty)
                        {
                            SetTile(new(x + d.x, y + d.y), GridType.Wall);
                            tileCount++;
                            hasCreatedWall = true;
                        }

                    if (hasCreatedWall)
                        if (wallStepTime.Type == WaitTime.TimeType.Second && wallStepTime.Second > 0)
                            yield return new WaitForSeconds(wallStepTime.Second);
                        else
                            for (int i = 0; i < wallStepTime.Frame; i++)
                                yield return null;
                }

        if (fillEmpty)
            CreateEmpty();
    }

    void CreateEmpty()
    {
        for (int x = 0; x < mapSize.x; x++)
            for (int y = 0; y < mapSize.y; y++)
                if (grid[x, y] == GridType.Empty)
                    SetTile(new(x, y), GridType.Empty);
    }

    void SetTile(Vector3Int pos, GridType type)
    {
        switch (type)
        {
            case GridType.Empty:
                tileMap.SetTile(pos, emptyTile); break;
            case GridType.Floor:
                tileMap.SetTile(pos, floorTile); break;
            case GridType.Wall:
                tileMap.SetTile(pos, wallTile); break;
        }
        grid[pos.x, pos.y] = type;
    }
}
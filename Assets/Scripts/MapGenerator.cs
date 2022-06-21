using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public int roomThresholdSize = 100;
    public int wallThresholdSize = 50;
    public string seed;
    public bool useRandomSeed;
    [Range(1, 10)]
    public int smoothingIterations;
    
    public int width;
    public int height;
    
    [Range(0,100)]
    public int randomFillPercent;
    
    private int[,] _map;
    public int borderSize;

    private void OnValidate()
    {
        GenerateMap();
    }

    private void Start()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        _map = new int[width, height];
        RandomFillMap();

        for (var i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }

        ProcessMap();
        
        var borderedMap = new int[width + borderSize * 2, height + borderSize * 2];
        
        for (var x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (var y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                {
                    borderedMap[x,y] = _map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
    }
    
    /// <summary>
    /// Detect Regions in the map, and change regions that have a size below a certain threshold to the opposite type.
    /// This results in a map, which doesn't contain regions that are too small.
    /// </summary>
    private void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);
        //Debug.LogFormat("{0} Wall Regions", wallRegions.Count);

        foreach (var wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (var tile in wallRegion)
                {
                    _map[tile.tileX, tile.tileY] = 0;
                }
            }
        }
        
        List<List<Coord>> roomRegions = GetRegions(0);
        //Debug.LogFormat("{0} Room Regions", wallRegions.Count);
        foreach (var roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (var tile in roomRegion)
                {
                    _map[tile.tileX, tile.tileY] = 1;
                }
            }
        }
    }

    /// <summary>
    /// Loop over the entire map and detect the regions that are contained.
    /// If a region has already been iterated over, it won't get iterated over again.
    /// </summary>
    /// <param name="tileType"></param>
    /// <returns></returns>
    private List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new();
        var mapFlags = new int[width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && _map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                    

                    foreach (var tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }


    /// <summary>
    /// This method returns an area connected to a given tile, via a flood-fill algorithm.
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="startY"></param>
    /// <returns></returns>
    private List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new();
        var mapFlags = new int[width, height];
        int tileType = _map[startX, startY];

        Queue<Coord> queue = new();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && _map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x,y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    private void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
        {
            //If the tile is on the edge of the map...
            if (x == 0 || x == width -1 || y == 0 || y == height - 1)
            {
                _map[x, y] = 1;
            }
            else
            {
                _map[x, y] = pseudoRandom.Next(0, 100) < randomFillPercent ? 1 : 0;
            }
        }
    }

    private void SmoothMap()
    {
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
        {
            int neighbourWallTiles = GetSurroundingWallCount(x, y);
            _map[x, y] = neighbourWallTiles > 4 ? 1 : 0;
        }
    }

    //Check neighbours for the tile that gets passed in, and see if they are walls or not.
    private int GetSurroundingWallCount(int gridX, int gridY)
    {
        var wallCount = 0;

        //Loop over neighbouring tiles.
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
        {
            //If neighbours are not above the edge of the map...
            if (IsInMapRange(neighbourX, neighbourY))
            {
                //...Or neighbour isn't the original tile...
                if (neighbourX != gridX || neighbourY != gridY)
                {
                    //...increment wall count only if the neighbour is a wall.
                    wallCount += _map[neighbourX, neighbourY]; // Returns either 0 or 1.
                } 
            }
                
            //If we are on the edge of the map it is considered a wall.
            else
            {
                wallCount++;
            }
        }

        return wallCount;
    }

    private bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            this.tileX = x;
            this.tileY = y;
        }
    }
    
}

using System;
using DefaultNamespace;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public string seed;
    public bool useRandomSeed;
    [Range(1, 10)]
    public int smoothingIterations;
    
    public int width;
    public int height;
    
    [Range(0,100)]
    public int randomFillPercent;
    
    private int[,] _map;

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

        int borderSize = 20;
        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
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
            if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
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

    //private void OnDrawGizmos()
    //{
    //    if (_map != null)
    //    {
    //        for (var x = 0; x < width; x++)
    //        for (var y = 0; y < height; y++)
    //        {
    //            Gizmos.color = _map[x, y] == 1 ? Color.black : Color.white;
    //            Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
    //            Gizmos.DrawCube(pos, Vector3.one);
    //        }
    //    }
    //}
}

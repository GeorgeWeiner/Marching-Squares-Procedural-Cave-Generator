using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace DefaultNamespace
{
    public class MeshGenerator : MonoBehaviour
    {
        public SquareGrid squareGrid;
        public MeshFilter walls;
        [HideInInspector] public List<Vector3> vertices;
        [HideInInspector] public List<int> triangles;

        private readonly Dictionary<int, List<Triangle>> triangleDictionary = new ();
        private readonly List<List<int>> outlines = new ();
        private readonly HashSet<int> checkedVertices = new ();

        public void GenerateMesh(int[,] map, float squareSize)
        {
            triangleDictionary.Clear();
            outlines.Clear();
            checkedVertices.Clear();

            squareGrid = new SquareGrid(map, squareSize);
            vertices = new List<Vector3>();
            triangles = new List<int>();

            for (var x = 0; x < squareGrid.squares.GetLength(0); x++)
            for (var y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x,y]);
            }
            

            Mesh mesh = new Mesh();
            GetComponent<MeshFilter>().sharedMesh = mesh;

            mesh.name = "Cave Mesh";
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            CreateWallMesh();
        }

        private void CreateWallMesh()
        {
            CalculateMeshOutlines();
            
            List<Vector3> wallVertices = new();
            List<int> wallTriangles = new();

            Mesh wallMesh = new();
            const float wallHeight = 5;

            foreach (List<int> outline in outlines)
            {
                for (var i = 0; i < outline.Count - 1; i++)
                {
                    int startIndex = wallVertices.Count;
                    wallVertices.Add(vertices[outline[i]]); //left vertex
                    wallVertices.Add(vertices[outline[i + 1]]); //right vertex
                    wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); //bottom left vertex
                    wallVertices.Add(vertices[outline[i + 1]]- Vector3.up * wallHeight); //bottom left vertex
                    
                    wallTriangles.Add(startIndex + 0);
                    wallTriangles.Add(startIndex + 2);
                    wallTriangles.Add(startIndex + 3);
                    
                    wallTriangles.Add(startIndex + 3);
                    wallTriangles.Add(startIndex + 1);
                    wallTriangles.Add(startIndex + 0);
                }
            }

            wallMesh.vertices = wallVertices.ToArray();
            wallMesh.triangles = wallTriangles.ToArray();
            wallMesh.RecalculateNormals();
            walls.sharedMesh = wallMesh;
        }

        private void TriangulateSquare(Square square)
        {
            switch (square.configuration)
            {
                case 0:
                    break;
                
                // 1 point:
                case 1:
                    MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                    break;
                case 2:
                    MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                    break;
                case 4:
                    MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                    break;
                case 8:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                    break;
                
                // 2 points:
                case 3:
                    MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                    break;
                case 6:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                    break;
                case 9:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                    break;
                case 12:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                    break;
                case 5:
                    MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                    break;
                case 10:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                    break;
                
                // 3 points:
                case 7:
                    MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                    break;
                case 11:
                    MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                    break;
                case 13:
                    MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                    break;
                case 14:
                    MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                    break;
                
                //4 points:
                case 15:
                    MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                    checkedVertices.Add(square.topLeft.vertexIndex);
                    checkedVertices.Add(square.topRight.vertexIndex);
                    checkedVertices.Add(square.bottomRight.vertexIndex);
                    checkedVertices.Add(square.bottomLeft.vertexIndex);
                    break;
                    
            }
        }

        private void MeshFromPoints(params Node[] points)
        {
            AssignVertices(points);
            
            if (points.Length >= 3)
                CreateTriangle(points[0], points[1], points[2]);
            if (points.Length >= 4)
                CreateTriangle(points[0], points[2], points[3]);
            if (points.Length >= 5)
                CreateTriangle(points[0], points[3], points[4]);
            if (points.Length >= 6)
                CreateTriangle(points[0], points[4], points[5]);
        }

        private void AssignVertices(Node[] points)
        {
            foreach (Node node in points)
            {
                if (node.vertexIndex == -1)
                {
                    node.vertexIndex = vertices.Count;
                    vertices.Add(node.pos); 
                }
            }
        }

        private void CreateTriangle(Node a, Node b, Node c)
        {
            triangles.Add(a.vertexIndex);
            triangles.Add(b.vertexIndex);
            triangles.Add(c.vertexIndex);

            Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
            AddTriangleToDictionary(triangle.vertexIndexA, triangle);
            AddTriangleToDictionary(triangle.vertexIndexB, triangle);
            AddTriangleToDictionary(triangle.vertexIndexC, triangle);
        }

        private int GetConnectedOutlineVertex(int vertexIndex)
        {
            List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];
            for (var i = 0; i < trianglesContainingVertex.Count; i++)
            {
                Triangle triangle = trianglesContainingVertex[i];

                for (var j = 0; j < 3; j++)
                {
                    int vertexB = triangle[j];

                    if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                    {
                        if (IsOutlineEdge(vertexIndex, vertexB))
                        {
                            return vertexB;
                        }
                    }
                }
            }

            return -1;
        }

        private bool IsOutlineEdge(int vertexA, int vertexB)
        {
            List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
            var sharedTriangleCount = 0;

            for (var i = 0; i < trianglesContainingVertexA.Count; i++)
            {
                if (trianglesContainingVertexA[i].Contains(vertexB))
                {
                    sharedTriangleCount++;
                    if (sharedTriangleCount > 1)
                    {
                        break;
                    }
                }
            }

            return sharedTriangleCount == 1;
        }

        private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
        {
            if (triangleDictionary.ContainsKey(vertexIndexKey))
            {
                triangleDictionary[vertexIndexKey].Add(triangle);
            }
            else
            {
                var triangleList = new List<Triangle> {triangle};
                triangleDictionary.Add(vertexIndexKey, triangleList);
            }
        }

        private void CalculateMeshOutlines()
        {
            for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                if (!checkedVertices.Contains(vertexIndex))
                {
                    int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                    if (newOutlineVertex != -1)
                    {
                        checkedVertices.Add(vertexIndex);

                        var newOutline = new List<int> {vertexIndex};
                        outlines.Add(newOutline);
                        FollowOutline(newOutlineVertex, outlines.Count - 1);
                        outlines[^1].Add(vertexIndex); //Count - 1
                    }
                }
            }
        }

        private void FollowOutline(int vertexIndex, int outlineIndex)
        {
            outlines[outlineIndex].Add(vertexIndex);
            checkedVertices.Add(vertexIndex);
            int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

            if (nextVertexIndex != -1)
            {
                FollowOutline(nextVertexIndex, outlineIndex);
            } 
        }

        private readonly struct Triangle
        {
            public readonly int vertexIndexA;
            public readonly int vertexIndexB;
            public readonly int vertexIndexC;

            private readonly int[] vertices;

            public Triangle(int a, int b, int c)
            {
                vertexIndexA = a;
                vertexIndexB = b;
                vertexIndexC = c;

                vertices = new[] {a, b, c};
            }

            public int this[int i] => vertices[i];

            public bool Contains(int vertexIndex)
            {
                return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
            }
        }


        public class SquareGrid
        {
            public readonly Square[,] squares;

            public SquareGrid(int[,] map, float squareSize)
            {
                int nodeCountX = map.GetLength(0);
                int nodeCountY = map.GetLength(1);
                float mapWidth = nodeCountX * squareSize;
                float mapHeight = nodeCountY * squareSize;

                var controlNodes = new ControlNode[nodeCountX, nodeCountY];

                for (var x = 0; x < nodeCountX; x++)
                {
                    for (var y = 0; y < nodeCountY; y++)
                    {
                        //Linearly increase the positional values, and start at an offset, so that the pivot of the mesh is centered.
                        Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0,
                            -mapHeight / 2 + y * squareSize + squareSize / 2);
                        //Construct the control node.
                        controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                    }
                }

                squares = new Square[nodeCountX - 1, nodeCountY - 1];
                
                for (var x = 0; x < nodeCountX - 1; x++)
                {
                    for (var y = 0; y < nodeCountY - 1; y++)
                    {
                        //Construct the square from the control nodes created above.
                        squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1],
                            controlNodes[x + 1, y], controlNodes[x, y]);
                    }
                }
            }
        }
        
        public class Square
        {
            public readonly ControlNode topLeft;
            public readonly ControlNode topRight;
            public readonly ControlNode bottomRight;
            public readonly ControlNode bottomLeft;
            
            public readonly Node centreTop;
            public readonly Node centreRight;
            public readonly Node centreBottom;
            public readonly Node centreLeft;
            
            public readonly int configuration;

            public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft)
            {
                //Corner Nodes
                topLeft = _topLeft;
                topRight = _topRight;
                bottomRight = _bottomRight;
                bottomLeft = _bottomLeft;

                //Edge Nodes
                centreTop = topLeft.right;
                centreRight = bottomRight.above;
                centreBottom = bottomLeft.right;
                centreLeft = bottomLeft.above;

                //For marching cubes replace this with left-wise bit shift.
                if (topLeft.active) //1000
                    configuration += 8;
                if (topRight.active) //0100
                    configuration += 4;
                if (bottomRight.active) //0010
                    configuration += 2;
                if (bottomLeft.active) //0001
                    configuration += 1;
            }
        }
        
        //Node along the edge
        public class Node
        {
            public Vector3 pos;
            public int vertexIndex = -1;

            protected internal Node(Vector3 _pos)
            {
                this.pos = _pos;
            }
        }
        
        //Node along the corners.
        public class ControlNode : Node
        {
            public readonly bool active;
            public readonly Node above;
            public readonly Node right;

            public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
            {
                this.active = _active;
                above = new Node(_pos + Vector3.forward * squareSize / 2f);
                right = new Node(_pos + Vector3.right * squareSize / 2f);
            }
        }
        
    }
}
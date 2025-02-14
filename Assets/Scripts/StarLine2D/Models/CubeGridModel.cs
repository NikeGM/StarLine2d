using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarLine2D.Models
{
    public class CubeGridModel
    {
        private int Radius { get; }
        private int Width { get; }
        private int Height { get; }
        public Dictionary<(int, int, int), CubeCellModel> Cells { get; }

        private static readonly (int, int, int)[] NeighborOffsets =
        {
            (1, -1, 0), (1, 0, -1), (0, 1, -1),
            (-1, 1, 0), (-1, 0, 1), (0, -1, 1)
        };

        public CubeGridModel(int radius)
        {
            Radius = radius;
            Cells = new Dictionary<(int, int, int), CubeCellModel>();

            GenerateGridByRadius();
        }

        public CubeGridModel(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new Dictionary<(int, int, int), CubeCellModel>();

            GenerateGrid();
        }

        private void GenerateGrid()
        {
            Cells.Clear();
            for (var r = 0; r < Height; r++)
            {
                for (var i = 0; i < Width; i++)
                {
                    var q = i - Mathf.FloorToInt(r / 2);
                    var s = -q - r;
                    Cells[(q, r, s)] = new CubeCellModel(q, r, s);
                }
            }
        }

        private void GenerateGridByRadius()
        {
            for (var q = -Radius; q <= Radius; q++)
            {
                for (var r = -Radius; r <= Radius; r++)
                {
                    var s = -r - q;
                    if (Mathf.Abs(s) <= Radius)
                    {
                        Cells.Add((q, r, s), new CubeCellModel(q, r, s));
                    }
                }
            }
        }

        public List<CubeCellModel> GetNeighbors(CubeCellModel cell)
        {
            List<CubeCellModel> neighbors = new();
            foreach (var offset in NeighborOffsets)
            {
                var (q, r, s) = (cell.Q + offset.Item1, cell.R + offset.Item2, cell.S + offset.Item3);
                if (Cells.TryGetValue((q, r, s), out var neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        public List<CubeCellModel> GetCellsInRadius(CubeCellModel centerCell, int radius)
        {
            var results = new List<CubeCellModel>();
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = Mathf.Max(-radius, -q - radius); r <= Mathf.Min(radius, -q + radius); r++)
                {
                    var s = -q - r;
                    if (Cells.TryGetValue((centerCell.Q + q, centerCell.R + r, centerCell.S + s), out var cell))
                    {
                        results.Add(cell);
                    }
                }
            }

            return results;
        }

        public CubeCellModel FindCellModel(int x, int y, int z)
        {
            Cells.TryGetValue((x, y, z), out var cell);
            return cell;
        }

        public List<CubeCellModel> FindShortestPath(CubeCellModel start, CubeCellModel end)
        {
            if (start == null || end == null) return null;

            var openSet = new HashSet<CubeCellModel> { start };
            var cameFrom = new Dictionary<CubeCellModel, CubeCellModel>();
            var gScore = Cells.ToDictionary(cell => cell.Value, _ => float.MaxValue);
            var fScore = Cells.ToDictionary(cell => cell.Value, _ => float.MaxValue);

            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);

            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(node => fScore[node]).First();

                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    var tentativeGScore = gScore[current] + 1; // Расстояние между соседними узлами всегда 1
                    if (tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return null; // Путь не найден
        }

        private float Heuristic(CubeCellModel a, CubeCellModel b)
        {
            // Манхэттенская дистанция для гексагональной сетки
            return (Mathf.Abs(a.Q - b.Q) + Mathf.Abs(a.R - b.R) + Mathf.Abs(a.S - b.S)) / 2f;
        }

        private List<CubeCellModel> ReconstructPath(Dictionary<CubeCellModel, CubeCellModel> cameFrom,
            CubeCellModel current)
        {
            var path = new List<CubeCellModel> { current };
            while (cameFrom.TryGetValue(current, out var previous))
            {
                path.Add(previous);
                current = previous;
            }

            path.Reverse();
            return path;
        }
        
        public List<CubeCellModel> GetLine(CubeCellModel start, CubeCellModel end)
        {
            var line = new List<CubeCellModel>();
            int distance = Mathf.Max(Mathf.Abs(start.Q - end.Q), Mathf.Abs(start.R - end.R), Mathf.Abs(start.S - end.S));

            for (int i = 0; i <= distance; i++)
            {
                float t = i / (float)distance;
                var lerpPoint = CubeLerp(start, end, t);
                var roundedPoint = CubeRound(lerpPoint);
        
                if (Cells.TryGetValue((roundedPoint.Q, roundedPoint.R, roundedPoint.S), out var cell))
                {
                    if (!line.Contains(cell))
                        line.Add(cell);
                }
            }

            return line;
        }

        private (float Q, float R, float S) CubeLerp(CubeCellModel a, CubeCellModel b, float t)
        {
            return (
                Mathf.Lerp(a.Q, b.Q, t),
                Mathf.Lerp(a.R, b.R, t),
                Mathf.Lerp(a.S, b.S, t)
            );
        }

        private (int Q, int R, int S) CubeRound((float Q, float R, float S) cube)
        {
            int q = Mathf.RoundToInt(cube.Q);
            int r = Mathf.RoundToInt(cube.R);
            int s = Mathf.RoundToInt(cube.S);

            float qDiff = Mathf.Abs(q - cube.Q);
            float rDiff = Mathf.Abs(r - cube.R);
            float sDiff = Mathf.Abs(s - cube.S);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                q = -r - s;
            }
            else if (rDiff > sDiff)
            {
                r = -q - s;
            }
            else
            {
                s = -q - r;
            }

            return (q, r, s);
        }
        
        public int GetDistance(CubeCellModel a, CubeCellModel b)
        {
            return (Mathf.Abs(a.Q - b.Q) + Mathf.Abs(a.R - b.R) + Mathf.Abs(a.S - b.S)) / 2 + 1;
        }
    }
}
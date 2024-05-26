using System.Collections.Generic;
using UnityEngine;

namespace StarLine.Models
{
    public class CubeGridModel
    {
        private int Radius { get; }
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

            GenerateGrid();
        }

        private void GenerateGrid()
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
    }
}

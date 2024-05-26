namespace StarLine2D.Models
{
    public class CubeCellModel
    {
        public int R { get; private set; }
        public int S { get; private set; }
        public int Q { get; private set; }

        public CubeCellModel(int q, int r, int s)
        {
            R = r;
            S = s;
            Q = q;
        }
    }
}

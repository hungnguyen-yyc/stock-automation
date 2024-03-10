namespace Stock.Shared.Models
{
    public class Point
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }

        public Point(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Point point)
            {
                return point.X == X && point.Y == Y;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}

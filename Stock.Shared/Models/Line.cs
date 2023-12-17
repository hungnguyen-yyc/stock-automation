namespace Stock.Shared.Models
{
    public class Line
    {
        public Point Start { get; set; }
        public Point End { get; set; }

        public Line(Point start, Point end)
        {
            Start = start;
            End = end;
        }

        public decimal FindYAtX(decimal xTarget)
        {
            var slope = (End.Y - Start.Y) / (End.X - Start.X);
            var intercept = Start.Y - slope * Start.X;
            var yTarget = slope * xTarget + intercept;

            return yTarget;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Line line)
            {
                return line.Start.Equals(Start) && line.End.Equals(End);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }
    }
}

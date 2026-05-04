using System.Windows;
using System.Windows.Media;

namespace DrawMe.Models
{
    public class DrawingTriangle : DrawingShapeBase
    {
        public double X      { get; set; }
        public double Y      { get; set; }
        public double Width  { get; set; }
        public double Height { get; set; }

        public DrawingTriangle() { }

        public DrawingTriangle(Point p1, Point p2)
        {
            var r = Helpers.GeometryHelper.NormalizeRect(p1, p2);
            X = r.X; Y = r.Y; Width = r.Width; Height = r.Height;
        }

        public override Rect BoundingRect => new Rect(X, Y, Width, Height);

        // Sommet haut-centre, bas-gauche, bas-droite
        public Point[] GetPoints() => new[]
        {
            new Point(X + Width / 2, Y),
            new Point(X,             Y + Height),
            new Point(X + Width,     Y + Height)
        };

        public override bool HitTest(Point p)
        {
            var pts = GetPoints();
            return PointInTriangle(p, pts[0], pts[1], pts[2]);
        }

        private static bool PointInTriangle(Point p, Point a, Point b, Point c)
        {
            double d1 = Sign(p, a, b);
            double d2 = Sign(p, b, c);
            double d3 = Sign(p, c, a);
            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private static double Sign(Point p1, Point p2, Point p3)
            => (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);

        public override void Translate(double dx, double dy) { X += dx; Y += dy; }

        public override DrawingShapeBase Clone() =>
            new DrawingTriangle
            {
                Id              = Guid.NewGuid(),
                X = X, Y = Y, Width = Width, Height = Height,
                FillColorHex    = FillColorHex,
                StrokeColorHex  = StrokeColorHex,
                StrokeThickness = StrokeThickness,
                ZIndex          = ZIndex
            };
    }
}

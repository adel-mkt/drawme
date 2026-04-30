using System.Windows;
using System.Windows.Media;

namespace DrawMe.Models
{
    /// <summary>
    /// Forme Ligne : définie par un point de départ et un point d'arrivée.
    /// Le hit-test utilise la distance perpendiculaire au segment.
    /// </summary>
    public class DrawingLine : DrawingShapeBase
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }

        public DrawingLine() { }

        public DrawingLine(Point start, Point end)
        {
            X1 = start.X; Y1 = start.Y;
            X2 = end.X;   Y2 = end.Y;
        }

        /// <summary>Rectangle englobant la ligne (avec tolérance pour hit-test).</summary>
        public override Rect BoundingRect =>
            new Rect(
                new Point(Math.Min(X1, X2), Math.Min(Y1, Y2)),
                new Point(Math.Max(X1, X2), Math.Max(Y1, Y2)));

        /// <summary>
        /// Hit-test : le clic est accepté si la distance perpendiculaire
        /// au segment est inférieure à (épaisseur + 4) pixels.
        /// </summary>
        public override bool HitTest(Point p)
        {
            double tolerance = StrokeThickness / 2.0 + 4.0;
            return Helpers.GeometryHelper.DistanceToSegment(p, new Point(X1, Y1), new Point(X2, Y2)) <= tolerance;
        }

        public override void Translate(double dx, double dy)
        {
            X1 += dx; Y1 += dy;
            X2 += dx; Y2 += dy;
        }

        public override DrawingShapeBase Clone() =>
            new DrawingLine
            {
                Id             = Guid.NewGuid(),
                X1             = X1, Y1 = Y1, X2 = X2, Y2 = Y2,
                FillColorHex   = FillColorHex,
                StrokeColorHex = StrokeColorHex,
                StrokeThickness= StrokeThickness,
                ZIndex         = ZIndex
            };
    }
}

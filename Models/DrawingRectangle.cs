using System.Windows;

namespace DrawMe.Models
{
    /// <summary>
    /// Forme Rectangle : définie par sa position (X,Y) et ses dimensions (Width, Height).
    /// Toujours normalisé (largeur et hauteur positives).
    /// </summary>
    public class DrawingRectangle : DrawingShapeBase
    {
        public double X      { get; set; }
        public double Y      { get; set; }
        public double Width  { get; set; }
        public double Height { get; set; }

        public DrawingRectangle() { }

        public DrawingRectangle(Point p1, Point p2)
        {
            var r = Helpers.GeometryHelper.NormalizeRect(p1, p2);
            X = r.X; Y = r.Y; Width = r.Width; Height = r.Height;
        }

        public override Rect BoundingRect => new Rect(X, Y, Width, Height);

        /// <summary>Hit-test : le point est dans le rectangle ARGB ou proche du contour.</summary>
        public override bool HitTest(Point p)
        {
            var r = new Rect(X, Y, Width, Height);

            // Remplissage opaque → tout l'intérieur
            if (FillColorHex.StartsWith("#00") || FillColorHex == "Transparent")
            {
                // Pas de remplissage : tester le contour uniquement
                double tol = StrokeThickness / 2.0 + 3.0;
                return (p.X >= r.Left - tol && p.X <= r.Right  + tol &&
                        p.Y >= r.Top  - tol && p.Y <= r.Bottom + tol) &&
                       !(p.X > r.Left + tol && p.X < r.Right  - tol &&
                         p.Y > r.Top  + tol && p.Y < r.Bottom - tol);
            }
            return r.Contains(p);
        }

        public override void Translate(double dx, double dy)
        {
            X += dx; Y += dy;
        }

        public override DrawingShapeBase Clone() =>
            new DrawingRectangle
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

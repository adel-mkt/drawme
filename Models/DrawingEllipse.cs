using System.Windows;

namespace DrawMe.Models
{
    /// <summary>
    /// Forme Ellipse : définie par sa position (X,Y) et ses dimensions (Width, Height).
    /// Le hit-test utilise l'équation canonique de l'ellipse.
    /// </summary>
    public class DrawingEllipse : DrawingShapeBase
    {
        public double X      { get; set; }
        public double Y      { get; set; }
        public double Width  { get; set; }
        public double Height { get; set; }

        public DrawingEllipse() { }

        public DrawingEllipse(Point p1, Point p2)
        {
            var r = Helpers.GeometryHelper.NormalizeRect(p1, p2);
            X = r.X; Y = r.Y; Width = r.Width; Height = r.Height;
        }

        public override Rect BoundingRect => new Rect(X, Y, Width, Height);

        // Centre et demi-axes
        private double Cx => X + Width  / 2.0;
        private double Cy => Y + Height / 2.0;
        private double Rx => Width  / 2.0;
        private double Ry => Height / 2.0;

        /// <summary>
        /// Hit-test : équation de l'ellipse ((px-cx)²/rx² + (py-cy)²/ry² ≤ 1).
        /// </summary>
        public override bool HitTest(Point p)
        {
            if (Rx <= 0 || Ry <= 0) return false;
            double nx = (p.X - Cx) / Rx;
            double ny = (p.Y - Cy) / Ry;
            return (nx * nx + ny * ny) <= 1.0;
        }

        public override void Translate(double dx, double dy)
        {
            X += dx; Y += dy;
        }

        public override DrawingShapeBase Clone() =>
            new DrawingEllipse
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

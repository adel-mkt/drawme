using System.Windows;

namespace DrawMe.Helpers
{
    /// <summary>
    /// Utilitaires géométriques statiques.
    /// Centralisés ici pour éviter la duplication dans les classes de formes.
    /// </summary>
    public static class GeometryHelper
    {
        /// <summary>
        /// Normalise un rectangle défini par deux points quelconques
        /// (assure que Width et Height sont positifs).
        /// </summary>
        public static Rect NormalizeRect(Point p1, Point p2)
        {
            double x = Math.Min(p1.X, p2.X);
            double y = Math.Min(p1.Y, p2.Y);
            double w = Math.Abs(p2.X - p1.X);
            double h = Math.Abs(p2.Y - p1.Y);
            return new Rect(x, y, w, h);
        }

        /// <summary>
        /// Calcule la distance minimale d'un point P à un segment [A, B].
        /// </summary>
        public static double DistanceToSegment(Point p, Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;

            if (lenSq < 1e-9)
                // Segment dégénéré → distance au point A
                return Distance(p, a);

            // Paramètre de projection de P sur la droite AB (clamped → [0,1])
            double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq));

            Point projection = new Point(a.X + t * dx, a.Y + t * dy);
            return Distance(p, projection);
        }

        /// <summary>Distance euclidienne entre deux points.</summary>
        public static double Distance(Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Retourne vrai si le point est à l'intérieur (ou sur le bord) du rectangle,
        /// avec une tolérance optionnelle.
        /// </summary>
        public static bool PointInRect(Point p, Rect r, double tolerance = 0)
        {
            return p.X >= r.Left  - tolerance && p.X <= r.Right  + tolerance &&
                   p.Y >= r.Top   - tolerance && p.Y <= r.Bottom + tolerance;
        }
    }
}

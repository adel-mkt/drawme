using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace DrawMe.Models
{
    /// <summary>
    /// Enumération des types de formes supportés.
    /// Utilisée pour la désérialisation polymorphique JSON.
    /// </summary>
    public enum ShapeType
    {
        Line,
        Rectangle,
        Ellipse
    }

    /// <summary>
    /// Classe abstraite de base pour toutes les formes du canvas.
    /// Implémente les propriétés communes : couleurs, épaisseur, z-index, identifiant unique.
    /// Principe SOLID : Open/Closed — on étend sans modifier la base.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(DrawingLine),      typeDiscriminator: "line")]
    [JsonDerivedType(typeof(DrawingRectangle), typeDiscriminator: "rectangle")]
    [JsonDerivedType(typeof(DrawingEllipse),   typeDiscriminator: "ellipse")]
    public abstract class DrawingShapeBase
    {
        // ───────────────────────────────────────────────
        // Identifiant unique de la forme
        // ───────────────────────────────────────────────
        public Guid Id { get; set; } = Guid.NewGuid();

        // ───────────────────────────────────────────────
        // Apparence
        // ───────────────────────────────────────────────

        /// <summary>Couleur de remplissage (ARGB hex, ex: "#FF3498DB").</summary>
        public string FillColorHex { get; set; } = "#FF3498DB";

        /// <summary>Couleur du contour (ARGB hex).</summary>
        public string StrokeColorHex { get; set; } = "#FF2C3E50";

        /// <summary>Épaisseur du trait (px).</summary>
        public double StrokeThickness { get; set; } = 2.0;

        // ───────────────────────────────────────────────
        // Ordre d'affichage
        // ───────────────────────────────────────────────

        /// <summary>Z-index : valeur plus haute = au-dessus.</summary>
        public int ZIndex { get; set; } = 0;

        // ───────────────────────────────────────────────
        // Propriétés calculées (ignorées JSON)
        // ───────────────────────────────────────────────

        [JsonIgnore]
        public Color FillColor
        {
            get => (Color)ColorConverter.ConvertFromString(FillColorHex);
            set => FillColorHex = value.ToString();
        }

        [JsonIgnore]
        public Color StrokeColor
        {
            get => (Color)ColorConverter.ConvertFromString(StrokeColorHex);
            set => StrokeColorHex = value.ToString();
        }

        // ───────────────────────────────────────────────
        // Méthodes abstraites — polymorphisme
        // ───────────────────────────────────────────────

        /// <summary>Rectangle englobant la forme.</summary>
        public abstract Rect BoundingRect { get; }

        /// <summary>Test de clic : retourne vrai si le point appartient à la forme.</summary>
        public abstract bool HitTest(Point p);

        /// <summary>Déplace la forme d'un vecteur (dx, dy).</summary>
        public abstract void Translate(double dx, double dy);

        /// <summary>Clone la forme (copie profonde).</summary>
        public abstract DrawingShapeBase Clone();
    }
}

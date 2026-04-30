namespace DrawMe.Models
{
    /// <summary>
    /// Document de dessin sérialisable en JSON.
    /// Contient toutes les formes et les métadonnées du fichier.
    /// </summary>
    public class DrawingDocument
    {
        /// <summary>Version du format de fichier (pour compatibilité future).</summary>
        public string Version { get; set; } = "1.0";

        /// <summary>Date de création / dernière modification.</summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>Titre du dessin.</summary>
        public string Title { get; set; } = "Nouveau dessin";

        /// <summary>Liste des formes (polymorphisme géré par [JsonDerivedType]).</summary>
        public List<DrawingShapeBase> Shapes { get; set; } = new List<DrawingShapeBase>();
    }
}

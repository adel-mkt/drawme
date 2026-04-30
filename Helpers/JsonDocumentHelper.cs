using DrawMe.Models;
using System.IO;
using System.Text.Json;

namespace DrawMe.Helpers
{
    /// <summary>
    /// Gestion de la sauvegarde et du chargement de documents au format JSON.
    /// Utilise System.Text.Json avec support du polymorphisme via [JsonDerivedType].
    /// </summary>
    public static class JsonDocumentHelper
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented      = true,
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>Sauvegarde un DrawingDocument dans un fichier JSON.</summary>
        /// <param name="path">Chemin complet du fichier de destination.</param>
        /// <param name="document">Le document à sérialiser.</param>
        public static void Save(string path, DrawingDocument document)
        {
            document.LastModified = DateTime.Now;
            string json = JsonSerializer.Serialize(document, _options);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Charge un DrawingDocument depuis un fichier JSON.
        /// Les types concrets sont restaurés grâce aux discriminants JSON ($type).
        /// </summary>
        /// <param name="path">Chemin complet du fichier source.</param>
        /// <returns>Le document chargé, ou null en cas d'erreur.</returns>
        public static DrawingDocument? Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Fichier introuvable : {path}");

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DrawingDocument>(json, _options);
        }
    }
}

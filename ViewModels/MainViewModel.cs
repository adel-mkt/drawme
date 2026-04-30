using DrawMe.Commands;
using DrawMe.Helpers;
using DrawMe.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace DrawMe.ViewModels
{
    /// <summary>
    /// ViewModel principal (MVVM).
    /// Expose toutes les propriétés liables à la vue (MainWindow).
    /// Délègue la logique canvas à DrawingCanvas et les commandes à DrawingCommandManager.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // ─── Commandes / historique ────────────────────────────────────────
        public DrawingCommandManager CommandManager { get; } = new();

        // ─── Collection de formes ──────────────────────────────────────────
        public ObservableCollection<DrawingShapeBase> Shapes { get; } = new();

        // ─── Forme sélectionnée ────────────────────────────────────────────
        private DrawingShapeBase? _selectedShape;
        public DrawingShapeBase? SelectedShape
        {
            get => _selectedShape;
            set { _selectedShape = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelection)); }
        }

        public bool HasSelection => _selectedShape != null;

        // ─── Outil actif ───────────────────────────────────────────────────
        public enum Tool { Pointer, Line, Rectangle, Ellipse }

        private Tool _currentTool = Tool.Pointer;
        public Tool CurrentTool
        {
            get => _currentTool;
            set { _currentTool = value; OnPropertyChanged(); }
        }

        // ─── Couleur de remplissage ────────────────────────────────────────
        private Color _fillColor = Colors.SteelBlue;
        public Color FillColor
        {
            get => _fillColor;
            set { _fillColor = value; OnPropertyChanged(); }
        }

        // ─── Couleur de contour ────────────────────────────────────────────
        private Color _strokeColor = Colors.DarkSlateGray;
        public Color StrokeColor
        {
            get => _strokeColor;
            set { _strokeColor = value; OnPropertyChanged(); }
        }

        // ─── Épaisseur du trait ────────────────────────────────────────────
        private double _strokeThickness = 2.0;
        public double StrokeThickness
        {
            get => _strokeThickness;
            set { _strokeThickness = Math.Max(0.5, Math.Min(30, value)); OnPropertyChanged(); }
        }

        // ─── Zoom ──────────────────────────────────────────────────────────
        private double _zoomFactor = 1.0;
        public double ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                _zoomFactor = Math.Max(0.1, Math.Min(5.0, value));
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomPercent));
            }
        }
        public string ZoomPercent => $"{(int)(_zoomFactor * 100)}%";

        // ─── Chemin du fichier courant ─────────────────────────────────────
        private string? _currentFilePath;
        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set { _currentFilePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(WindowTitle)); }
        }

        public string WindowTitle =>
            _currentFilePath != null
                ? $"DrawMe — {System.IO.Path.GetFileName(_currentFilePath)}"
                : "DrawMe — Nouveau dessin";

        // ─── Barre de statut ───────────────────────────────────────────────
        private string _statusMessage = "Prêt";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ─── Raccourcis commandes ──────────────────────────────────────────
        public bool CanUndo => CommandManager.CanUndo;
        public bool CanRedo => CommandManager.CanRedo;

        public MainViewModel()
        {
            CommandManager.HistoryChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            };
        }

        // ─── Sauvegarde ────────────────────────────────────────────────────
        public void SaveToFile(string path)
        {
            var doc = new DrawingDocument
            {
                Title  = System.IO.Path.GetFileNameWithoutExtension(path),
                Shapes = Shapes.ToList()
            };
            JsonDocumentHelper.Save(path, doc);
            CurrentFilePath = path;
            StatusMessage   = $"Sauvegardé : {System.IO.Path.GetFileName(path)}";
        }

        // ─── Chargement ────────────────────────────────────────────────────
        public void LoadFromFile(string path)
        {
            var doc = JsonDocumentHelper.Load(path);
            if (doc == null) return;

            Shapes.Clear();
            foreach (var shape in doc.Shapes)
                Shapes.Add(shape);

            SelectedShape   = null;
            CurrentFilePath = path;
            StatusMessage   = $"Chargé : {System.IO.Path.GetFileName(path)} ({doc.Shapes.Count} formes)";
        }

        // ─── Nouveau dessin ────────────────────────────────────────────────
        public void NewDocument()
        {
            Shapes.Clear();
            SelectedShape   = null;
            CurrentFilePath = null;
            CommandManager.Clear();
            StatusMessage   = "Nouveau dessin créé";
        }

        // ─── INotifyPropertyChanged ────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using DrawMe.Commands;
using DrawMe.ViewModels;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawMe.Views
{
    /// <summary>
    /// Code-behind de la fenêtre principale.
    /// Responsabilités : câblage ViewModel ↔ Vue, gestion des boîtes de dialogue,
    /// raccourcis clavier, et délégation des actions au ViewModel.
    /// Principe de séparation : la logique métier reste dans le ViewModel et le DrawingCanvas.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            // Instanciation et injection du ViewModel
            _vm         = new MainViewModel();
            DataContext = _vm;

            // Connecter le canvas au ViewModel
            MainDrawingCanvas.SetViewModel(_vm);

            // Initialiser le refresh côté canvas après chargement
            Loaded += (_, _) =>
            {
                MainDrawingCanvas.RefreshCanvas();
                UpdateToolButtons();
                _vm.StatusMessage = "Prêt — Sélectionnez un outil et dessinez sur le canvas";
            };

            // Réaction aux changements de propriétés du VM
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentTool))
                    UpdateToolButtons();
                if (e.PropertyName == nameof(MainViewModel.SelectedShape))
                    UpdateSelectionInfoLabel();
                if (e.PropertyName == nameof(MainViewModel.FillColor) ||
                    e.PropertyName == nameof(MainViewModel.StrokeColor))
                    UpdateColorButtons();
            };
        }

        // ───────────────────────────────────────────────────────────────────
        // Mise à jour visuelle des boutons d'outils
        // ───────────────────────────────────────────────────────────────────

        private void UpdateToolButtons()
        {
            var active   = Application.Current.Resources["ActiveToolButtonStyle"]   as System.Windows.Style;
            var normal   = Application.Current.Resources["ToolButtonStyle"]         as System.Windows.Style;

            BtnToolPointer.Style = _vm.CurrentTool == MainViewModel.Tool.Pointer   ? active : normal;
            BtnToolLine.Style    = _vm.CurrentTool == MainViewModel.Tool.Line      ? active : normal;
            BtnToolRect.Style    = _vm.CurrentTool == MainViewModel.Tool.Rectangle ? active : normal;
            BtnToolEllipse.Style = _vm.CurrentTool == MainViewModel.Tool.Ellipse   ? active : normal;

            // Curseur adapté au canvas
            MainDrawingCanvas.Cursor = _vm.CurrentTool switch
            {
                MainViewModel.Tool.Pointer   => Cursors.Arrow,
                MainViewModel.Tool.Line      => Cursors.Cross,
                MainViewModel.Tool.Rectangle => Cursors.Cross,
                MainViewModel.Tool.Ellipse   => Cursors.Cross,
                _                            => Cursors.Arrow
            };
        }

        private void UpdateColorButtons()
        {
            BtnFillColor.Background   = new SolidColorBrush(_vm.FillColor);
            BtnStrokeColor.Background = new SolidColorBrush(_vm.StrokeColor);
        }

        private void UpdateSelectionInfoLabel()
        {
            if (_vm.SelectedShape == null)
            {
                TxtSelectionInfo.Text = "Aucune sélection";
                return;
            }
            var s = _vm.SelectedShape;
            var r = s.BoundingRect;
            TxtSelectionInfo.Text = $"{s.GetType().Name.Replace("Drawing", "")} — " +
                                    $"Pos: ({r.X:F0}, {r.Y:F0}) | Taille: {r.Width:F0}×{r.Height:F0}";
        }

        // ───────────────────────────────────────────────────────────────────
        // Outils
        // ───────────────────────────────────────────────────────────────────

        private void BtnToolPointer_Click(object sender, RoutedEventArgs e) =>
            _vm.CurrentTool = MainViewModel.Tool.Pointer;

        private void BtnToolLine_Click(object sender, RoutedEventArgs e) =>
            _vm.CurrentTool = MainViewModel.Tool.Line;

        private void BtnToolRect_Click(object sender, RoutedEventArgs e) =>
            _vm.CurrentTool = MainViewModel.Tool.Rectangle;

        private void BtnToolEllipse_Click(object sender, RoutedEventArgs e) =>
            _vm.CurrentTool = MainViewModel.Tool.Ellipse;

        // ───────────────────────────────────────────────────────────────────
        // Couleurs
        // ───────────────────────────────────────────────────────────────────

        private void BtnFillColor_Click(object sender, RoutedEventArgs e)
        {
            var color = ShowColorDialog(_vm.FillColor);
            if (color.HasValue) _vm.FillColor = color.Value;
        }

        private void BtnStrokeColor_Click(object sender, RoutedEventArgs e)
        {
            var color = ShowColorDialog(_vm.StrokeColor);
            if (color.HasValue) _vm.StrokeColor = color.Value;
        }

        private void BtnApplyColorToSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedShape == null) return;

            var cmd = new ChangeColorCommand(
                _vm.SelectedShape,
                _vm.FillColor.ToString(),
                _vm.StrokeColor.ToString());

            _vm.CommandManager.Execute(cmd);
            MainDrawingCanvas.RefreshShape(_vm.SelectedShape);
            _vm.StatusMessage = "Couleur appliquée à la sélection";
        }

        /// <summary>
        /// Ouvre un ColorDialog WPF natif (via System.Windows.Forms.ColorDialog).
        /// En WPF pur, on doit référencer WinForms pour le sélecteur de couleur natif Windows.
        /// Alternative : implémenter un ColorPicker XAML custom.
        /// </summary>
        private Color? ShowColorDialog(Color current)
        {
            var dlg = new System.Windows.Forms.ColorDialog
            {
                Color            = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B),
                FullOpen         = true,
                AllowFullOpen    = true,
                AnyColor         = true
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dlg.Color;
                return Color.FromArgb(c.A, c.R, c.G, c.B);
            }
            return null;
        }

        // ───────────────────────────────────────────────────────────────────
        // Édition
        // ───────────────────────────────────────────────────────────────────

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            _vm.CommandManager.Undo();
            MainDrawingCanvas.RefreshCanvas();
            _vm.StatusMessage = "Annulation";
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            _vm.CommandManager.Redo();
            MainDrawingCanvas.RefreshCanvas();
            _vm.StatusMessage = "Rétablissement";
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedShape == null) return;
            var cmd = new DeleteShapeCommand(_vm.Shapes, _vm.SelectedShape);
            _vm.CommandManager.Execute(cmd);
            _vm.SelectedShape = null;
            _vm.StatusMessage = "Forme supprimée";
        }

        // ───────────────────────────────────────────────────────────────────
        // Z-Index
        // ───────────────────────────────────────────────────────────────────

        private void BtnBringFront_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedShape == null) return;
            var cmd = new ChangeZIndexCommand(_vm.Shapes, _vm.SelectedShape, bringToFront: true);
            _vm.CommandManager.Execute(cmd);
            // Mettre à jour les ZIndex sur les modèles
            RecomputeZIndices();
            MainDrawingCanvas.RefreshCanvas();
            _vm.StatusMessage = "Mis au premier plan";
        }

        private void BtnSendBack_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedShape == null) return;
            var cmd = new ChangeZIndexCommand(_vm.Shapes, _vm.SelectedShape, bringToFront: false);
            _vm.CommandManager.Execute(cmd);
            RecomputeZIndices();
            MainDrawingCanvas.RefreshCanvas();
            _vm.StatusMessage = "Mis à l'arrière-plan";
        }

        /// <summary>
        /// Synchronise les Z-Index des modèles avec leur position dans la collection.
        /// La collection est ordonnée par ordre d'empilement (index = Z-index).
        /// </summary>
        private void RecomputeZIndices()
        {
            for (int i = 0; i < _vm.Shapes.Count; i++)
                _vm.Shapes[i].ZIndex = i;
        }

        // ───────────────────────────────────────────────────────────────────
        // Zoom
        // ───────────────────────────────────────────────────────────────────

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)  => _vm.ZoomFactor += 0.1;
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => _vm.ZoomFactor -= 0.1;
        private void BtnZoomReset_Click(object sender, RoutedEventArgs e) => _vm.ZoomFactor = 1.0;

        // ───────────────────────────────────────────────────────────────────
        // Fichier : Nouveau / Sauvegarder / Charger
        // ───────────────────────────────────────────────────────────────────

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.Shapes.Count > 0)
            {
                var result = MessageBox.Show(
                    "Créer un nouveau dessin ? Les modifications non sauvegardées seront perdues.",
                    "DrawMe — Nouveau", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            _vm.NewDocument();
            MainDrawingCanvas.RefreshCanvas();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string? path = _vm.CurrentFilePath;

            if (path == null)
            {
                // SaveAs
                var dlg = new SaveFileDialog
                {
                    Title      = "Sauvegarder le dessin",
                    Filter     = "Dessin DrawMe (*.drawme)|*.drawme|Fichier JSON (*.json)|*.json",
                    DefaultExt = ".drawme",
                    FileName   = "MonDessin"
                };
                if (dlg.ShowDialog() != true) return;
                path = dlg.FileName;
            }

            try
            {
                _vm.SaveToFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde :\n{ex.Message}",
                    "DrawMe — Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Ouvrir un dessin",
                Filter = "Dessin DrawMe (*.drawme)|*.drawme|Fichier JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                _vm.LoadFromFile(dlg.FileName);
                MainDrawingCanvas.RefreshCanvas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement :\n{ex.Message}",
                    "DrawMe — Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // Raccourcis clavier (niveau fenêtre)
        // ───────────────────────────────────────────────────────────────────

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            // Ctrl+Z : Undo
            if (ctrl && e.Key == Key.Z) { BtnUndo_Click(sender, new RoutedEventArgs()); e.Handled = true; return; }

            // Ctrl+Y : Redo
            if (ctrl && e.Key == Key.Y) { BtnRedo_Click(sender, new RoutedEventArgs()); e.Handled = true; return; }

            // Ctrl+S : Save
            if (ctrl && e.Key == Key.S) { BtnSave_Click(sender, new RoutedEventArgs()); e.Handled = true; return; }

            // Ctrl+O : Open
            if (ctrl && e.Key == Key.O) { BtnLoad_Click(sender, new RoutedEventArgs()); e.Handled = true; return; }

            // Ctrl+N : New
            if (ctrl && e.Key == Key.N) { BtnNew_Click(sender, new RoutedEventArgs()); e.Handled = true; return; }

            // Ctrl++ : Zoom in
            if (ctrl && e.Key == Key.OemPlus)  { _vm.ZoomFactor += 0.1; e.Handled = true; return; }

            // Ctrl+- : Zoom out
            if (ctrl && e.Key == Key.OemMinus) { _vm.ZoomFactor -= 0.1; e.Handled = true; return; }

            // Ctrl+0 : Zoom reset
            if (ctrl && e.Key == Key.D0) { _vm.ZoomFactor = 1.0; e.Handled = true; return; }

            // Ctrl+] : Bring to front
            if (ctrl && e.Key == Key.OemCloseBrackets) { BtnBringFront_Click(sender, new RoutedEventArgs()); e.Handled = true; return; }

            // Ctrl+[ : Send to back
            if (ctrl && e.Key == Key.OemOpenBrackets)  { BtnSendBack_Click(sender, new RoutedEventArgs()); e.Handled = true; return; }

            // A : Appliquer couleur à la sélection
            if (e.Key == Key.A && !ctrl && _vm.HasSelection) { BtnApplyColorToSelection_Click(sender, new RoutedEventArgs()); e.Handled = true; return; }

            // Raccourcis outils (sans modificateurs)
            if (!ctrl)
            {
                switch (e.Key)
                {
                    case Key.V: _vm.CurrentTool = MainViewModel.Tool.Pointer;   e.Handled = true; break;
                    case Key.L: _vm.CurrentTool = MainViewModel.Tool.Line;      e.Handled = true; break;
                    case Key.R: _vm.CurrentTool = MainViewModel.Tool.Rectangle; e.Handled = true; break;
                    case Key.E: _vm.CurrentTool = MainViewModel.Tool.Ellipse;   e.Handled = true; break;
                    case Key.Delete:
                        BtnDelete_Click(sender, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}

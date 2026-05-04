using DrawMe.Commands;
using DrawMe.ViewModels;
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

        private Action<Color>? _colorPickerCallback;

        public MainWindow()
        {
            InitializeComponent();

            // Instanciation et injection du ViewModel
            _vm         = new MainViewModel();
            DataContext = _vm;

            // Connecter le canvas au ViewModel
            MainDrawingCanvas.SetViewModel(_vm);

            // Color picker : live update + recalcul des thumbs à l'ouverture
            ColorPicker.ColorChanged += color => _colorPickerCallback?.Invoke(color);
            ColorPickerPopupCtrl.Opened += (_, _) => ColorPicker.Refresh();

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

                if (!_vm.IsUpdatingFromShape && _vm.SelectedShape != null &&
                    (e.PropertyName == nameof(MainViewModel.FillColor) ||
                     e.PropertyName == nameof(MainViewModel.StrokeColor) ||
                     e.PropertyName == nameof(MainViewModel.StrokeThickness)))
                {
                    _vm.SelectedShape.FillColorHex    = _vm.FillColor.ToString();
                    _vm.SelectedShape.StrokeColorHex  = _vm.StrokeColor.ToString();
                    _vm.SelectedShape.StrokeThickness = _vm.StrokeThickness;
                    MainDrawingCanvas.RefreshShape(_vm.SelectedShape);
                }
            };
        }

        // ───────────────────────────────────────────────────────────────────
        // Mise à jour visuelle des boutons d'outils
        // ───────────────────────────────────────────────────────────────────

        private void UpdateToolButtons()
        {
            var active   = Application.Current.Resources["ActiveToolButtonStyle"]   as System.Windows.Style;
            var normal   = Application.Current.Resources["ToolButtonStyle"]         as System.Windows.Style;

            BtnToolPointer.Style  = _vm.CurrentTool == MainViewModel.Tool.Pointer   ? active : normal;
            BtnToolLine.Style     = _vm.CurrentTool == MainViewModel.Tool.Line      ? active : normal;
            BtnToolRect.Style     = _vm.CurrentTool == MainViewModel.Tool.Rectangle ? active : normal;
            BtnToolEllipse.Style  = _vm.CurrentTool == MainViewModel.Tool.Ellipse   ? active : normal;
            BtnToolTriangle.Style = _vm.CurrentTool == MainViewModel.Tool.Triangle  ? active : normal;

            MainDrawingCanvas.Cursor = _vm.CurrentTool switch
            {
                MainViewModel.Tool.Pointer  => Cursors.Arrow,
                _                           => Cursors.Cross
            };
        }

        private void UpdateColorButtons()
        {
            BtnFillColor.Background   = new SolidColorBrush(_vm.FillColor);
            BtnStrokeColor.Background = new SolidColorBrush(_vm.StrokeColor);
        }

        private void UpdateSelectionInfoLabel()
        {
            if (_vm.SelectedShape == null) return;
            var s = _vm.SelectedShape;
            var r = s.BoundingRect;
            _vm.StatusMessage = $"{s.GetType().Name.Replace("Drawing", "")}  •  " +
                                $"{r.Width:F0} × {r.Height:F0} px  •  ({r.X:F0}, {r.Y:F0})";
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

        private void BtnToolTriangle_Click(object sender, RoutedEventArgs e) =>
            _vm.CurrentTool = MainViewModel.Tool.Triangle;

        // ───────────────────────────────────────────────────────────────────
        // Couleurs
        // ───────────────────────────────────────────────────────────────────

        private void BtnFillColor_Click(object sender, RoutedEventArgs e) =>
            OpenColorPicker(BtnFillColor, _vm.FillColor, c => _vm.FillColor = c);

        private void BtnStrokeColor_Click(object sender, RoutedEventArgs e) =>
            OpenColorPicker(BtnStrokeColor, _vm.StrokeColor, c => _vm.StrokeColor = c);

        private void OpenColorPicker(FrameworkElement target, Color current, Action<Color> callback)
        {
            ColorPicker.SelectedColor = current;
            ColorPickerPopupCtrl.PlacementTarget = target;
            _colorPickerCallback = callback;

            // Placer au-dessus si pas assez de place en dessous
            var pt      = target.PointToScreen(new Point(0, target.ActualHeight));
            var workH   = SystemParameters.WorkArea.Bottom;
            ColorPickerPopupCtrl.Placement      = pt.Y + 340 > workH
                ? System.Windows.Controls.Primitives.PlacementMode.Top
                : System.Windows.Controls.Primitives.PlacementMode.Bottom;
            ColorPickerPopupCtrl.VerticalOffset = 4;
            ColorPickerPopupCtrl.IsOpen = true;
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


        // ───────────────────────────────────────────────────────────────────
        // Édition
        // ───────────────────────────────────────────────────────────────────

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            _vm.CommandManager.Undo();
            RecomputeZIndices();
            MainDrawingCanvas.RefreshCanvas();
            _vm.StatusMessage = "Annulation";
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            _vm.CommandManager.Redo();
            RecomputeZIndices();
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
                    case Key.T: _vm.CurrentTool = MainViewModel.Tool.Triangle;  e.Handled = true; break;
                    case Key.Delete:
                        BtnDelete_Click(sender, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}

using DrawMe.Commands;
using DrawMe.Models;
using DrawMe.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DrawMe.Views
{
    /// <summary>
    /// Canvas de dessin principal.
    /// Hérite de Canvas (WPF).
    /// Gère : dessin, sélection, déplacement, redimensionnement, zoom.
    /// Architecture : la logique est ici car elle est fortement couplée aux événements souris WPF.
    /// </summary>
    public class DrawingCanvas : Canvas
    {
        // ───────────────────────────────────────────────────────────────────
        // Constantes UI
        // ───────────────────────────────────────────────────────────────────
        private const double HandleSize    = 10.0;  // Taille des poignées (pixels)
        private const double HandleHalf    = HandleSize / 2.0;
        private const double MinShapeSize  = 4.0;   // Taille minimale d'une forme

        // ───────────────────────────────────────────────────────────────────
        // Référence au ViewModel
        // ───────────────────────────────────────────────────────────────────
        private MainViewModel? _vm;

        // ───────────────────────────────────────────────────────────────────
        // Dictionnaire forme modèle → élément WPF
        // ───────────────────────────────────────────────────────────────────
        private readonly Dictionary<DrawingShapeBase, UIElement> _shapeElements = new();

        // ───────────────────────────────────────────────────────────────────
        // État de l'interaction souris
        // ───────────────────────────────────────────────────────────────────
        private enum MouseMode { None, Drawing, Moving, Resizing }
        private MouseMode _mouseMode = MouseMode.None;

        private Point _mouseDownPoint;      // Point de départ du drag
        private Point _lastMousePoint;      // Point souris précédent (pour delta)

        // ─── Dessin en cours ───────────────────────────────────────────────
        private UIElement? _previewElement; // Forme en cours de tracé (feedback visuel)

        // ─── Déplacement ───────────────────────────────────────────────────
        private DrawingShapeBase? _movingShape;
        private Point _moveStartPosition;   // Position de la forme au début du drag

        // ─── Redimensionnement ─────────────────────────────────────────────
        private DrawingShapeBase? _resizingShape;
        private int _resizeHandleIndex;     // Quel handle (0-7)
        private ShapeSnapshot? _resizeSnapshot; // État avant resize

        // ─── Sélection ─────────────────────────────────────────────────────
        private System.Windows.Shapes.Rectangle? _selectionRect; // Bordure de sélection
        private readonly List<Thumb> _handles = new();            // 8 poignées

        // ─── Zoom ──────────────────────────────────────────────────────────
        private ScaleTransform _scaleTransform = new(1, 1);

        // ───────────────────────────────────────────────────────────────────
        // Initialisation
        // ───────────────────────────────────────────────────────────────────

        public DrawingCanvas()
        {
            Background   = Brushes.White;
            ClipToBounds = true;
            Focusable    = true;

            // Zoom via ScaleTransform sur le canvas
            LayoutTransform = _scaleTransform;

            // Créer la bordure de sélection (invisible au départ)
            _selectionRect = new System.Windows.Shapes.Rectangle
            {
                Stroke          = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                Fill            = new SolidColorBrush(Color.FromArgb(20, 52, 152, 219)),
                IsHitTestVisible= false,
                Visibility      = Visibility.Collapsed
            };
            Children.Add(_selectionRect);
            SetZIndex(_selectionRect, 9998);

            // Créer les 8 poignées de redimensionnement
            for (int i = 0; i < 8; i++)
            {
                var thumb = CreateResizeHandle(i);
                _handles.Add(thumb);
                Children.Add(thumb);
                SetZIndex(thumb, 9999);
            }

            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseMove           += OnMouseMove;
            MouseLeftButtonUp   += OnMouseLeftButtonUp;
            MouseWheel          += OnMouseWheel;
        }

        /// <summary>Injecte le ViewModel après construction.</summary>
        public void SetViewModel(MainViewModel vm)
        {
            _vm = vm;
            _vm.Shapes.CollectionChanged += (_, _) => RefreshCanvas();
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.ZoomFactor))
                {
                    _scaleTransform.ScaleX = _vm.ZoomFactor;
                    _scaleTransform.ScaleY = _vm.ZoomFactor;
                }
                if (e.PropertyName == nameof(MainViewModel.SelectedShape))
                    UpdateSelectionOverlay();
            };
        }

        // ───────────────────────────────────────────────────────────────────
        // Rendu complet du canvas (depuis la collection du ViewModel)
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reconstruit tous les éléments WPF à partir de la collection Shapes.
        /// Appelé après chaque changement de collection (add, remove, undo, load...).
        /// </summary>
        public void RefreshCanvas()
        {
            // Supprimer les anciens éléments de forme (garder les overlays)
            foreach (var el in _shapeElements.Values.ToList())
                Children.Remove(el);
            _shapeElements.Clear();

            if (_vm == null) return;

            // Trier par ZIndex pour l'affichage
            var ordered = _vm.Shapes.OrderBy(s => s.ZIndex).ToList();
            foreach (var shape in ordered)
            {
                var el = CreateWpfElement(shape);
                _shapeElements[shape] = el;
                Children.Add(el);
                SetZIndex(el, shape.ZIndex);
            }

            UpdateSelectionOverlay();
        }

        /// <summary>
        /// Met à jour uniquement un élément WPF existant selon son modèle.
        /// Plus efficace que RefreshCanvas() pour une mise à jour partielle.
        /// </summary>
        public void RefreshShape(DrawingShapeBase shape)
        {
            if (!_shapeElements.TryGetValue(shape, out var el)) return;
            ApplyModelToElement(shape, el);
            if (_vm?.SelectedShape == shape)
                UpdateSelectionOverlay();
        }

        // ───────────────────────────────────────────────────────────────────
        // Création d'éléments WPF depuis les modèles
        // ───────────────────────────────────────────────────────────────────

        private UIElement CreateWpfElement(DrawingShapeBase shape)
        {
            UIElement el = shape switch
            {
                DrawingLine      => new System.Windows.Shapes.Line(),
                DrawingRectangle => new System.Windows.Shapes.Rectangle(),
                DrawingEllipse   => new System.Windows.Shapes.Ellipse(),
                DrawingTriangle  => new System.Windows.Shapes.Polygon(),
                _                => throw new NotSupportedException()
            };
            ApplyModelToElement(shape, el);
            return el;
        }

        private static void ApplyModelToElement(DrawingShapeBase shape, UIElement el)
        {
            var fill   = new SolidColorBrush(shape.FillColor);
            var stroke = new SolidColorBrush(shape.StrokeColor);

            switch (shape, el)
            {
                case (DrawingLine m, System.Windows.Shapes.Line l):
                    l.X1 = m.X1; l.Y1 = m.Y1; l.X2 = m.X2; l.Y2 = m.Y2;
                    l.Stroke          = stroke;
                    l.StrokeThickness = m.StrokeThickness;
                    break;

                case (DrawingRectangle m, System.Windows.Shapes.Rectangle r):
                    SetLeft(r, m.X); SetTop(r, m.Y);
                    r.Width  = m.Width;
                    r.Height = m.Height;
                    r.Fill             = fill;
                    r.Stroke           = stroke;
                    r.StrokeThickness  = m.StrokeThickness;
                    break;

                case (DrawingEllipse m, System.Windows.Shapes.Ellipse e):
                    SetLeft(e, m.X); SetTop(e, m.Y);
                    e.Width  = m.Width;
                    e.Height = m.Height;
                    e.Fill             = fill;
                    e.Stroke           = stroke;
                    e.StrokeThickness  = m.StrokeThickness;
                    break;

                case (DrawingTriangle m, System.Windows.Shapes.Polygon p):
                    p.Points          = new PointCollection(m.GetRenderPoints(m.StrokeThickness));
                    p.Fill            = fill;
                    p.Stroke          = stroke;
                    p.StrokeThickness = m.StrokeThickness;
                    p.StrokeLineJoin  = PenLineJoin.Round;
                    break;
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // Poignées de redimensionnement
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Crée un Thumb (poignée de redim) pour l'index donné.
        /// Ordre des indices :
        ///   0=TopLeft, 1=TopCenter, 2=TopRight,
        ///   3=MiddleLeft,            4=MiddleRight,
        ///   5=BottomLeft, 6=BottomCenter, 7=BottomRight
        /// </summary>
        private Thumb CreateResizeHandle(int index)
        {
            var cursor = index switch
            {
                0 => Cursors.SizeNWSE,
                1 => Cursors.SizeNS,
                2 => Cursors.SizeNESW,
                3 => Cursors.SizeWE,
                4 => Cursors.SizeWE,
                5 => Cursors.SizeNESW,
                6 => Cursors.SizeNS,
                7 => Cursors.SizeNWSE,
                _ => Cursors.Arrow
            };

            var thumb = new Thumb
            {
                Width      = HandleSize,
                Height     = HandleSize,
                Cursor     = cursor,
                Visibility = Visibility.Collapsed,
                Template   = CreateHandleTemplate()
            };

            thumb.DragStarted  += (_, e) => OnHandleDragStarted(index, thumb);
            thumb.DragDelta    += (_, e) => OnHandleDragDelta(index, e);
            thumb.DragCompleted+= (_, e) => OnHandleDragCompleted(index);

            return thumb;
        }

        private static ControlTemplate CreateHandleTemplate()
        {
            // Carré blanc avec bordure bleue
            var template = new ControlTemplate(typeof(Thumb));
            var border   = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty,    Brushes.White);
            border.SetValue(Border.BorderBrushProperty,   new SolidColorBrush(Color.FromRgb(231, 76, 60)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(2));
            border.SetValue(Border.CornerRadiusProperty,  new CornerRadius(2));
            template.VisualTree = border;
            return template;
        }

        // ───────────────────────────────────────────────────────────────────
        // Overlay de sélection
        // ───────────────────────────────────────────────────────────────────

        private void UpdateSelectionOverlay()
        {
            var shape = _vm?.SelectedShape;

            if (shape == null)
            {
                _selectionRect!.Visibility = Visibility.Collapsed;
                foreach (var h in _handles) h.Visibility = Visibility.Collapsed;
                return;
            }

            var r = shape.BoundingRect;

            // Bordure de sélection
            _selectionRect!.Visibility = Visibility.Visible;
            SetLeft(_selectionRect, r.X - 4);
            SetTop(_selectionRect,  r.Y - 4);
            _selectionRect.Width  = r.Width  + 8;
            _selectionRect.Height = r.Height + 8;

            // Positions des 8 poignées autour du bounding rect
            var positions = GetHandlePositions(r);
            for (int i = 0; i < 8; i++)
            {
                _handles[i].Visibility = Visibility.Visible;
                SetLeft(_handles[i], positions[i].X - HandleHalf);
                SetTop (_handles[i], positions[i].Y - HandleHalf);
            }
        }

        private static Point[] GetHandlePositions(Rect r)
        {
            double mx = r.X + r.Width  / 2;
            double my = r.Y + r.Height / 2;
            return new[]
            {
                new Point(r.Left,  r.Top),    // 0 TL
                new Point(mx,      r.Top),    // 1 TC
                new Point(r.Right, r.Top),    // 2 TR
                new Point(r.Left,  my),       // 3 ML
                new Point(r.Right, my),       // 4 MR
                new Point(r.Left,  r.Bottom), // 5 BL
                new Point(mx,      r.Bottom), // 6 BC
                new Point(r.Right, r.Bottom)  // 7 BR
            };
        }

        // ───────────────────────────────────────────────────────────────────
        // Événements souris
        // ───────────────────────────────────────────────────────────────────

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm == null) return;

            Focus();
            _mouseDownPoint = e.GetPosition(this);
            _lastMousePoint = _mouseDownPoint;

            if (_vm.CurrentTool == MainViewModel.Tool.Pointer)
            {
                // ── Mode Sélection/Déplacement ──────────────────────────────
                var hit = HitTestShape(_mouseDownPoint);
                _vm.SelectedShape = hit;

                if (hit != null)
                {
                    // Début de déplacement
                    _mouseMode        = MouseMode.Moving;
                    _movingShape      = hit;
                    _moveStartPosition = _mouseDownPoint;
                    CaptureMouse();
                }
                else
                {
                    _mouseMode = MouseMode.None;
                }
            }
            else
            {
                // ── Mode Dessin ─────────────────────────────────────────────
                _mouseMode = MouseMode.Drawing;
                _previewElement = CreatePreviewElement();
                if (_previewElement != null)
                {
                    Children.Add(_previewElement);
                    SetZIndex(_previewElement, 10000);
                }
                CaptureMouse();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_vm == null) return;

            var current = e.GetPosition(this);

            switch (_mouseMode)
            {
                case MouseMode.Drawing:
                    UpdatePreviewElement(_mouseDownPoint, current);
                    break;

                case MouseMode.Moving when _movingShape != null:
                    double dx = current.X - _lastMousePoint.X;
                    double dy = current.Y - _lastMousePoint.Y;
                    _movingShape.Translate(dx, dy);
                    RefreshShape(_movingShape);
                    break;

                case MouseMode.Resizing:
                    // Géré dans OnHandleDragDelta
                    break;
            }

            // Curseur contextuel en mode Pointer
            if (_vm.CurrentTool == MainViewModel.Tool.Pointer && _mouseMode == MouseMode.None)
            {
                var hit = HitTestShape(current);
                Cursor = hit != null ? Cursors.SizeAll : Cursors.Arrow;
            }

            _lastMousePoint = current;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_vm == null) return;

            var current = e.GetPosition(this);
            ReleaseMouseCapture();

            switch (_mouseMode)
            {
                case MouseMode.Drawing:
                    CommitDrawing(_mouseDownPoint, current);
                    RemovePreviewElement();
                    break;

                case MouseMode.Moving when _movingShape != null:
                    // Commit la commande de déplacement dans l'historique
                    double totalDx = current.X - _moveStartPosition.X;
                    double totalDy = current.Y - _moveStartPosition.Y;

                    if (Math.Abs(totalDx) > 0.5 || Math.Abs(totalDy) > 0.5)
                    {
                        // Remettre à la position initiale pour refaire le déplacement proprement via command
                        _movingShape.Translate(-totalDx, -totalDy);
                        var cmd = new MoveShapeCommand(_movingShape, totalDx, totalDy);
                        _vm.CommandManager.Execute(cmd);
                        RefreshShape(_movingShape);
                        _vm.StatusMessage = $"Déplacé de ({totalDx:F0}, {totalDy:F0})";
                    }
                    _movingShape = null;
                    break;
            }

            _mouseMode = MouseMode.None;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_vm == null) return;

            // Ctrl + molette = zoom
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                double delta = e.Delta > 0 ? 0.1 : -0.1;
                _vm.ZoomFactor += delta;
                e.Handled = true;
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // Dessin des formes
        // ───────────────────────────────────────────────────────────────────

        /// <summary>Crée l'élément de prévisualisation (pendant le tracé).</summary>
        private UIElement? CreatePreviewElement()
        {
            if (_vm == null) return null;
            var fill   = new SolidColorBrush(_vm.FillColor)   { Opacity = 0.5 };
            var stroke = new SolidColorBrush(_vm.StrokeColor) { Opacity = 0.7 };

            return _vm.CurrentTool switch
            {
                MainViewModel.Tool.Line =>
                    new System.Windows.Shapes.Line
                    {
                        Stroke          = stroke,
                        StrokeThickness = _vm.StrokeThickness,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    },
                MainViewModel.Tool.Rectangle =>
                    new System.Windows.Shapes.Rectangle
                    {
                        Fill = fill, Stroke = stroke,
                        StrokeThickness = _vm.StrokeThickness,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    },
                MainViewModel.Tool.Ellipse =>
                    new System.Windows.Shapes.Ellipse
                    {
                        Fill = fill, Stroke = stroke,
                        StrokeThickness = _vm.StrokeThickness,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    },
                MainViewModel.Tool.Triangle =>
                    new System.Windows.Shapes.Polygon
                    {
                        Fill = fill, Stroke = stroke,
                        StrokeThickness = _vm.StrokeThickness,
                        StrokeLineJoin  = PenLineJoin.Round,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    },
                _ => null
            };
        }

        private void UpdatePreviewElement(Point p1, Point p2)
        {
            if (_previewElement == null) return;
            var r = Helpers.GeometryHelper.NormalizeRect(p1, p2);

            switch (_previewElement)
            {
                case System.Windows.Shapes.Line line:
                    line.X1 = p1.X; line.Y1 = p1.Y;
                    line.X2 = p2.X; line.Y2 = p2.Y;
                    break;
                case System.Windows.Shapes.Rectangle rect:
                    SetLeft(rect, r.X); SetTop(rect, r.Y);
                    rect.Width  = Math.Max(1, r.Width);
                    rect.Height = Math.Max(1, r.Height);
                    break;
                case System.Windows.Shapes.Ellipse ell:
                    SetLeft(ell, r.X); SetTop(ell, r.Y);
                    ell.Width  = Math.Max(1, r.Width);
                    ell.Height = Math.Max(1, r.Height);
                    break;
                case System.Windows.Shapes.Polygon poly:
                    var tmp = new DrawingTriangle { X = r.X, Y = r.Y, Width = Math.Max(1, r.Width), Height = Math.Max(1, r.Height), StrokeThickness = poly.StrokeThickness };
                    poly.Points = new PointCollection(tmp.GetRenderPoints(poly.StrokeThickness));
                    break;
            }
        }

        private void RemovePreviewElement()
        {
            if (_previewElement != null)
            {
                Children.Remove(_previewElement);
                _previewElement = null;
            }
        }

        /// <summary>Valide le dessin et crée la forme dans le ViewModel.</summary>
        private void CommitDrawing(Point p1, Point p2)
        {
            if (_vm == null) return;

            var r = Helpers.GeometryHelper.NormalizeRect(p1, p2);

            // Évite les formes minuscules (clic accidentel)
            bool tooSmall = (_vm.CurrentTool != MainViewModel.Tool.Line) &&
                            (r.Width < MinShapeSize || r.Height < MinShapeSize);
            bool isLineTooShort = (_vm.CurrentTool == MainViewModel.Tool.Line) &&
                                  Helpers.GeometryHelper.Distance(p1, p2) < MinShapeSize;
            if (tooSmall || isLineTooShort) return;

            DrawingShapeBase shape = _vm.CurrentTool switch
            {
                MainViewModel.Tool.Line =>
                    new DrawingLine(p1, p2)
                    {
                        FillColorHex    = _vm.FillColor.ToString(),
                        StrokeColorHex  = _vm.StrokeColor.ToString(),
                        StrokeThickness = _vm.StrokeThickness,
                        ZIndex          = _vm.Shapes.Count
                    },
                MainViewModel.Tool.Rectangle =>
                    new DrawingRectangle(p1, p2)
                    {
                        FillColorHex    = _vm.FillColor.ToString(),
                        StrokeColorHex  = _vm.StrokeColor.ToString(),
                        StrokeThickness = _vm.StrokeThickness,
                        ZIndex          = _vm.Shapes.Count
                    },
                MainViewModel.Tool.Ellipse =>
                    new DrawingEllipse(p1, p2)
                    {
                        FillColorHex    = _vm.FillColor.ToString(),
                        StrokeColorHex  = _vm.StrokeColor.ToString(),
                        StrokeThickness = _vm.StrokeThickness,
                        ZIndex          = _vm.Shapes.Count
                    },
                MainViewModel.Tool.Triangle =>
                    new DrawingTriangle(p1, p2)
                    {
                        FillColorHex    = _vm.FillColor.ToString(),
                        StrokeColorHex  = _vm.StrokeColor.ToString(),
                        StrokeThickness = _vm.StrokeThickness,
                        ZIndex          = _vm.Shapes.Count
                    },
                _ => throw new NotSupportedException()
            };

            _vm.CommandManager.Execute(new AddShapeCommand(_vm.Shapes, shape));
            _vm.SelectedShape = shape;
            _vm.StatusMessage = $"{shape.GetType().Name.Replace("Drawing","")} ajouté";
        }

        // ───────────────────────────────────────────────────────────────────
        // Gestion des poignées de redimensionnement (Thumb drag)
        // ───────────────────────────────────────────────────────────────────

        private void OnHandleDragStarted(int index, Thumb thumb)
        {
            _resizingShape    = _vm?.SelectedShape;
            _resizeHandleIndex= index;
            _resizeSnapshot   = _resizingShape != null ? ShapeSnapshot.From(_resizingShape) : null;
            _mouseMode        = MouseMode.Resizing;

        }

        private void OnHandleDragDelta(int index, DragDeltaEventArgs e)
        {
            if (_resizingShape == null) return;

            double dx = e.HorizontalChange;
            double dy = e.VerticalChange;

            ApplyResizeDelta(_resizingShape, index, dx, dy);
            RefreshShape(_resizingShape);
        }

        private void OnHandleDragCompleted(int index)
        {
            if (_resizingShape == null || _resizeSnapshot == null) return;

            var after = ShapeSnapshot.From(_resizingShape);
            var cmd   = new ResizeShapeCommand(_resizingShape, _resizeSnapshot, after);
            _vm?.CommandManager.PushResizeCommand(cmd);

            _resizingShape  = null;
            _resizeSnapshot = null;
            _mouseMode      = MouseMode.None;
            _vm!.StatusMessage = "Redimensionné";
        }

        /// <summary>
        /// Applique un delta de redimensionnement selon l'index de la poignée.
        /// Garantit une taille minimale et adapte la logique selon le type de forme.
        /// </summary>
        private void ApplyResizeDelta(DrawingShapeBase shape, int handleIndex, double dx, double dy)
        {
            switch (shape)
            {
                case DrawingLine line:
                {
                    bool affectsLeft   = handleIndex == 0 || handleIndex == 3 || handleIndex == 5;
                    bool affectsRight  = handleIndex == 2 || handleIndex == 4 || handleIndex == 7;
                    bool affectsTop    = handleIndex == 0 || handleIndex == 1 || handleIndex == 2;
                    bool affectsBottom = handleIndex == 5 || handleIndex == 6 || handleIndex == 7;

                    double newX1 = line.X1, newY1 = line.Y1;
                    double newX2 = line.X2, newY2 = line.Y2;

                    if (affectsLeft)
                    {
                        if (line.X1 <= line.X2) newX1 = Math.Min(line.X1 + dx, line.X2 - MinShapeSize);
                        else                     newX2 = Math.Min(line.X2 + dx, line.X1 - MinShapeSize);
                    }
                    if (affectsRight)
                    {
                        if (line.X1 >= line.X2) newX1 = Math.Max(line.X1 + dx, line.X2 + MinShapeSize);
                        else                     newX2 = Math.Max(line.X2 + dx, line.X1 + MinShapeSize);
                    }
                    if (affectsTop)
                    {
                        if (line.Y1 <= line.Y2) newY1 = Math.Min(line.Y1 + dy, line.Y2 - MinShapeSize);
                        else                     newY2 = Math.Min(line.Y2 + dy, line.Y1 - MinShapeSize);
                    }
                    if (affectsBottom)
                    {
                        if (line.Y1 >= line.Y2) newY1 = Math.Max(line.Y1 + dy, line.Y2 + MinShapeSize);
                        else                     newY2 = Math.Max(line.Y2 + dy, line.Y1 + MinShapeSize);
                    }

                    line.X1 = newX1; line.Y1 = newY1;
                    line.X2 = newX2; line.Y2 = newY2;
                    break;
                }

                case DrawingRectangle rect:
                {
                    bool left   = handleIndex == 0 || handleIndex == 3 || handleIndex == 5;
                    bool right  = handleIndex == 2 || handleIndex == 4 || handleIndex == 7;
                    bool top    = handleIndex == 0 || handleIndex == 1 || handleIndex == 2;
                    bool bottom = handleIndex == 5 || handleIndex == 6 || handleIndex == 7;

                    if (left)  { double nw = rect.Width  - dx; if (nw > MinShapeSize) { rect.X += dx; rect.Width  = nw; } }
                    if (right) { double nw = rect.Width  + dx; if (nw > MinShapeSize)               rect.Width  = nw;   }
                    if (top)   { double nh = rect.Height - dy; if (nh > MinShapeSize) { rect.Y += dy; rect.Height = nh; } }
                    if (bottom){ double nh = rect.Height + dy; if (nh > MinShapeSize)               rect.Height = nh;   }
                    break;
                }

                case DrawingEllipse ell:
                {
                    bool left   = handleIndex == 0 || handleIndex == 3 || handleIndex == 5;
                    bool right  = handleIndex == 2 || handleIndex == 4 || handleIndex == 7;
                    bool top    = handleIndex == 0 || handleIndex == 1 || handleIndex == 2;
                    bool bottom = handleIndex == 5 || handleIndex == 6 || handleIndex == 7;

                    if (left)  { double nw = ell.Width  - dx; if (nw > MinShapeSize) { ell.X += dx; ell.Width  = nw; } }
                    if (right) { double nw = ell.Width  + dx; if (nw > MinShapeSize)               ell.Width  = nw;   }
                    if (top)   { double nh = ell.Height - dy; if (nh > MinShapeSize) { ell.Y += dy; ell.Height = nh; } }
                    if (bottom){ double nh = ell.Height + dy; if (nh > MinShapeSize)               ell.Height = nh;   }
                    break;
                }

                case DrawingTriangle tri:
                {
                    bool left   = handleIndex == 0 || handleIndex == 3 || handleIndex == 5;
                    bool right  = handleIndex == 2 || handleIndex == 4 || handleIndex == 7;
                    bool top    = handleIndex == 0 || handleIndex == 1 || handleIndex == 2;
                    bool bottom = handleIndex == 5 || handleIndex == 6 || handleIndex == 7;

                    if (left)  { double nw = tri.Width  - dx; if (nw > MinShapeSize) { tri.X += dx; tri.Width  = nw; } }
                    if (right) { double nw = tri.Width  + dx; if (nw > MinShapeSize)               tri.Width  = nw;   }
                    if (top)   { double nh = tri.Height - dy; if (nh > MinShapeSize) { tri.Y += dy; tri.Height = nh; } }
                    if (bottom){ double nh = tri.Height + dy; if (nh > MinShapeSize)               tri.Height = nh;   }
                    break;
                }
            }
        }


        // ───────────────────────────────────────────────────────────────────
        // Hit-test : trouver la forme sous le curseur
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Retourne la forme visible (Z-index le plus haut) sous le point donné.
        /// </summary>
        private DrawingShapeBase? HitTestShape(Point p)
        {
            if (_vm == null) return null;

            // Parcours en ordre inverse (plus haut ZIndex = premier plan = prioritaire)
            return _vm.Shapes
                .OrderByDescending(s => s.ZIndex)
                .FirstOrDefault(s => s.HitTest(p));
        }

        // ───────────────────────────────────────────────────────────────────
        // Suppression clavier
        // ───────────────────────────────────────────────────────────────────

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_vm == null) return;

            if (e.Key == Key.Delete && _vm.SelectedShape != null)
            {
                var cmd = new DeleteShapeCommand(_vm.Shapes, _vm.SelectedShape);
                _vm.CommandManager.Execute(cmd);
                _vm.SelectedShape  = null;
                _vm.StatusMessage  = "Forme supprimée";
                e.Handled          = true;
            }
        }
    }
}

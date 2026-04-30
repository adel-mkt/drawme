using DrawMe.Models;
using System.Collections.ObjectModel;

namespace DrawMe.Commands
{
    /// <summary>
    /// Commande : ajout d'une forme au canvas.
    /// Undo → supprime la forme. Redo → la réajoute.
    /// </summary>
    public class AddShapeCommand : IDrawingCommand
    {
        private readonly ObservableCollection<DrawingShapeBase> _shapes;
        private readonly DrawingShapeBase _shape;

        public string Description => $"Ajouter {_shape.GetType().Name}";

        public AddShapeCommand(ObservableCollection<DrawingShapeBase> shapes, DrawingShapeBase shape)
        {
            _shapes = shapes;
            _shape  = shape;
        }

        public void Execute() => _shapes.Add(_shape);
        public void Undo()    => _shapes.Remove(_shape);
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Commande : suppression d'une forme du canvas.
    /// </summary>
    public class DeleteShapeCommand : IDrawingCommand
    {
        private readonly ObservableCollection<DrawingShapeBase> _shapes;
        private readonly DrawingShapeBase _shape;
        private int _indexSnapshot;

        public string Description => $"Supprimer {_shape.GetType().Name}";

        public DeleteShapeCommand(ObservableCollection<DrawingShapeBase> shapes, DrawingShapeBase shape)
        {
            _shapes = shapes;
            _shape  = shape;
        }

        public void Execute()
        {
            _indexSnapshot = _shapes.IndexOf(_shape);
            _shapes.Remove(_shape);
        }

        public void Undo()
        {
            // Réinsère à la même position si possible
            if (_indexSnapshot >= 0 && _indexSnapshot <= _shapes.Count)
                _shapes.Insert(_indexSnapshot, _shape);
            else
                _shapes.Add(_shape);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Commande : déplacement d'une forme (drag &amp; drop).
    /// Stocke le delta de déplacement pour pouvoir annuler.
    /// </summary>
    public class MoveShapeCommand : IDrawingCommand
    {
        private readonly DrawingShapeBase _shape;
        private readonly double _dx;
        private readonly double _dy;

        public string Description => $"Déplacer {_shape.GetType().Name}";

        public MoveShapeCommand(DrawingShapeBase shape, double dx, double dy)
        {
            _shape = shape;
            _dx    = dx;
            _dy    = dy;
        }

        public void Execute() => _shape.Translate( _dx,  _dy);
        public void Undo()    => _shape.Translate(-_dx, -_dy);
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Commande : changement de couleur de remplissage / contour d'une forme.
    /// </summary>
    public class ChangeColorCommand : IDrawingCommand
    {
        private readonly DrawingShapeBase _shape;
        private readonly string _oldFill;
        private readonly string _newFill;
        private readonly string _oldStroke;
        private readonly string _newStroke;

        public string Description => "Changer couleur";

        public ChangeColorCommand(DrawingShapeBase shape,
                                  string newFill, string newStroke)
        {
            _shape     = shape;
            _oldFill   = shape.FillColorHex;
            _oldStroke = shape.StrokeColorHex;
            _newFill   = newFill;
            _newStroke = newStroke;
        }

        public void Execute()
        {
            _shape.FillColorHex   = _newFill;
            _shape.StrokeColorHex = _newStroke;
        }

        public void Undo()
        {
            _shape.FillColorHex   = _oldFill;
            _shape.StrokeColorHex = _oldStroke;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Commande : modification du Z-index (bring to front / send to back).
    /// </summary>
    public class ChangeZIndexCommand : IDrawingCommand
    {
        private readonly ObservableCollection<DrawingShapeBase> _shapes;
        private readonly DrawingShapeBase _shape;
        private readonly bool _bringToFront;
        private int _oldIndex;

        public string Description => _bringToFront ? "Mettre au premier plan" : "Mettre à l'arrière-plan";

        public ChangeZIndexCommand(ObservableCollection<DrawingShapeBase> shapes,
                                   DrawingShapeBase shape, bool bringToFront)
        {
            _shapes       = shapes;
            _shape        = shape;
            _bringToFront = bringToFront;
        }

        public void Execute()
        {
            _oldIndex = _shapes.IndexOf(_shape);
            _shapes.Remove(_shape);

            if (_bringToFront)
                _shapes.Add(_shape); // fin = au-dessus
            else
                _shapes.Insert(0, _shape); // début = en-dessous
        }

        public void Undo()
        {
            _shapes.Remove(_shape);
            if (_oldIndex >= 0 && _oldIndex <= _shapes.Count)
                _shapes.Insert(_oldIndex, _shape);
            else
                _shapes.Add(_shape);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Commande : redimensionnement d'une forme via une poignée (handle).
    /// Stocke l'état complet avant/après.
    /// </summary>
    public class ResizeShapeCommand : IDrawingCommand
    {
        private readonly DrawingShapeBase _shape;
        private readonly ShapeSnapshot _before;
        private readonly ShapeSnapshot _after;

        public string Description => $"Redimensionner {_shape.GetType().Name}";

        public ResizeShapeCommand(DrawingShapeBase shape, ShapeSnapshot before, ShapeSnapshot after)
        {
            _shape  = shape;
            _before = before;
            _after  = after;
        }

        public void Execute() => _after.ApplyTo(_shape);
        public void Undo()    => _before.ApplyTo(_shape);
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Instantané de l'état géométrique d'une forme avant/après resize.
    /// </summary>
    public class ShapeSnapshot
    {
        public double X1, Y1, X2, Y2;           // Pour Line
        public double X, Y, Width, Height;       // Pour Rect/Ellipse

        public static ShapeSnapshot From(DrawingShapeBase shape)
        {
            var s = new ShapeSnapshot();
            switch (shape)
            {
                case Models.DrawingLine line:
                    s.X1 = line.X1; s.Y1 = line.Y1;
                    s.X2 = line.X2; s.Y2 = line.Y2;
                    break;
                case Models.DrawingRectangle rect:
                    s.X = rect.X; s.Y = rect.Y;
                    s.Width = rect.Width; s.Height = rect.Height;
                    break;
                case Models.DrawingEllipse ell:
                    s.X = ell.X; s.Y = ell.Y;
                    s.Width = ell.Width; s.Height = ell.Height;
                    break;
            }
            return s;
        }

        public void ApplyTo(DrawingShapeBase shape)
        {
            switch (shape)
            {
                case Models.DrawingLine line:
                    line.X1 = X1; line.Y1 = Y1;
                    line.X2 = X2; line.Y2 = Y2;
                    break;
                case Models.DrawingRectangle rect:
                    rect.X = X; rect.Y = Y;
                    rect.Width = Width; rect.Height = Height;
                    break;
                case Models.DrawingEllipse ell:
                    ell.X = X; ell.Y = Y;
                    ell.Width = Width; ell.Height = Height;
                    break;
            }
        }
    }
}

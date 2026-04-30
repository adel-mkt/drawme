namespace DrawMe.Commands
{
    /// <summary>
    /// Gestionnaire centralisé de l'historique des commandes (Undo/Redo).
    /// Implémente deux piles : une pour annuler (undo) et une pour rétablir (redo).
    /// Pattern : Command Manager / History Manager.
    /// </summary>
    public class DrawingCommandManager
    {
        private readonly Stack<IDrawingCommand> _undoStack = new();
        private readonly Stack<IDrawingCommand> _redoStack = new();

        // Limite de l'historique (évite les fuites mémoire)
        public int MaxHistorySize { get; set; } = 100;

        /// <summary>Vrai si on peut annuler une action.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>Vrai si on peut rétablir une action.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>Événement déclenché après chaque changement d'état.</summary>
        public event EventHandler? HistoryChanged;

        /// <summary>
        /// Exécute une commande et l'ajoute à la pile Undo.
        /// Vide la pile Redo (toute nouvelle action invalide le redo).
        /// </summary>
        public void Execute(IDrawingCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();

            // Limite l'historique pour ne pas consommer trop de mémoire
            while (_undoStack.Count > MaxHistorySize)
            {
                // On ne peut pas retirer le bas de la pile directement →
                // reconstruction (rare, seulement quand on dépasse la limite)
                var temp = _undoStack.ToArray();
                _undoStack.Clear();
                foreach (var c in temp.Take(MaxHistorySize).Reverse())
                    _undoStack.Push(c);
            }

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Annule la dernière action.</summary>
        public void Undo()
        {
            if (!CanUndo) return;
            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Rétablit la dernière action annulée.</summary>
        public void Redo()
        {
            if (!CanRedo) return;
            var cmd = _redoStack.Pop();
            cmd.Execute();
            _undoStack.Push(cmd);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Pousse une commande dans la pile Undo SANS l'exécuter.
        /// Utilisé pour les opérations appliquées en temps réel (ex: resize via Thumb).
        /// </summary>
        public void PushResizeCommand(IDrawingCommand command)
        {
            _undoStack.Push(command);
            _redoStack.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Vide tout l'historique (après Save ou New).</summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

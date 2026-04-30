namespace DrawMe.Commands
{
    /// <summary>
    /// Interface du Command Pattern pour Undo/Redo.
    /// Chaque opération utilisateur est encapsulée dans un IDrawingCommand.
    /// </summary>
    public interface IDrawingCommand
    {
        /// <summary>Exécute la commande.</summary>
        void Execute();

        /// <summary>Annule la commande (restaure l'état précédent).</summary>
        void Undo();

        /// <summary>Description lisible (pour debug et log).</summary>
        string Description { get; }
    }
}

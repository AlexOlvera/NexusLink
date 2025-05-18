using System;

namespace NexusLink.Context
{
    /// <summary>
    /// Proporciona un ámbito para operaciones en una base de datos específica
    /// </summary>
    public class DatabaseScope : IDisposable
    {
        private readonly string _previousDatabase;
        private readonly bool _ownsContext;

        private DatabaseScope(string databaseName, bool ownsContext)
        {
            _previousDatabase = DatabaseContext.Current.CurrentDatabaseName;
            _ownsContext = ownsContext;

            // Cambiar al nuevo contexto
            DatabaseContext.Current.CurrentDatabaseName = databaseName;
        }

        /// <summary>
        /// Crea un nuevo ámbito de base de datos
        /// </summary>
        public static DatabaseScope Create(string databaseName)
        {
            return new DatabaseScope(databaseName, false);
        }

        /// <summary>
        /// Crea un nuevo ámbito con un nuevo contexto
        /// </summary>
        public static DatabaseScope CreateNew(string databaseName)
        {
            DatabaseContext.Current = new DatabaseContext();
            return new DatabaseScope(databaseName, true);
        }

        public void Dispose()
        {
            // Restaurar el contexto anterior
            if (_ownsContext)
                DatabaseContext.Current.Clear();
            else
                DatabaseContext.Current.CurrentDatabaseName = _previousDatabase;
        }
    }
}
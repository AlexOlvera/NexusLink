using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace NexusLink
{
    public class NexusLinkDbContext : DbContext
    {
        private readonly DatabaseSelector _databaseSelector;

        public NexusLinkDbContext(DatabaseSelector databaseSelector)
            : base("Default")
        {
            _databaseSelector = databaseSelector;

            // Configura el DbContext para usar la conexión actual
            this.Database.Connection.ConnectionString =
                _databaseSelector.CurrentConnection.ConnectionString;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Registra configuraciones desde atributos NexusLink
            modelBuilder.Configurations.Add(new NexusLinkEntityConfigurator());

            base.OnModelCreating(modelBuilder);
        }

        // Override para soportar cambio de base de datos
        public void UseDatabase(string databaseName)
        {
            _databaseSelector.CurrentDatabaseName = databaseName;
            this.Database.Connection.ConnectionString =
                _databaseSelector.CurrentConnection.ConnectionString;
        }

        // Métodos para trabajar con transacciones NexusLink
        public void UseTransaction(System.Transactions.Transaction transaction)
        {
            this.Database.UseTransaction(
                transaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete)
                as SqlTransaction);
        }
    }
}

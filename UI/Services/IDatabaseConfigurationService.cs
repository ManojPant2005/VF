using System.Collections.Generic;
using System.Threading.Tasks;

namespace UI.Services
{
    public interface IDatabaseConfigurationService
    {
        Task<bool> CreateDatabaseAsync(string serverName, string databaseName, string connectionString, bool trustServerCertificate);
        Task<bool> CreateTableAsync(string serverName, string databaseName, string tableName, IEnumerable<string> columns);
        Task<bool> CreateStoredProceduresAsync(string serverName, string databaseName);
    }
}

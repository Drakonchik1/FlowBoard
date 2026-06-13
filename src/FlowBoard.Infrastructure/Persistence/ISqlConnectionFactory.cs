using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FlowBoard.Infrastructure.Persistence;

/// <summary>
/// Creates raw ADO.NET connections for the Dapper read side. Kept separate from the EF
/// DbContext so read queries can run on their own short-lived connection.
/// </summary>
internal interface ISqlConnectionFactory
{
    DbConnection Create();
}

internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public DbConnection Create() => new SqlConnection(_connectionString);
}

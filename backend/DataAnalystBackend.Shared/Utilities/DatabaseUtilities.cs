using Npgsql;

namespace DataAnalystBackend.Shared.Utilities
{
    public class DatabaseUtilities
    {
        public static async Task ExecuteSqlOnOtherDatabaseAsync(string connectionString, string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

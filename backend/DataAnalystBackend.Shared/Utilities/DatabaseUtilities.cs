using Npgsql;
using System.Text;

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

        public static async Task<string> RunSelectDelimitedAsync(string connectionString, string selectQuery)
        {
            var sb = new StringBuilder();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(selectQuery, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    // Rows
                    while (await reader.ReadAsync())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            sb.Append(await reader.IsDBNullAsync(i) ? "" : reader.GetValue(i).ToString());
                            if (i < reader.FieldCount - 1)
                                sb.Append("||");
                        }
                        sb.Append("[|]");
                    }
                }
            }

            // Remove the last [|] if needed
            if (sb.Length >= 3 && sb.ToString().EndsWith("[|]"))
                sb.Length -= 3;

            return sb.ToString();
        }
    }
}

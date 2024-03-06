using Npgsql;

namespace ContactPro.Helpers
{
    public static class ConnectionHelper
    {
        public static string? GetConnectionString(IConfiguration config)
        {
            var connectionString = config.GetSection("pgSettings")["pgConnection"];
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            return string.IsNullOrEmpty(databaseUrl) ? connectionString : BuildConnectionString(databaseUrl);
        }
        public static string BuildConnectionString(string databaseUrl)
        {
            var databaseUri = new Uri(databaseUrl);

            var userInfo = databaseUri.UserInfo.Split(":");

            var builder = new NpgsqlConnectionStringBuilder()
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require
            };

            return builder.ToString();
        }
    }
}

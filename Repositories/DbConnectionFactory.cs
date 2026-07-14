using MySqlConnector;
using System.Data;

namespace KhoQuanLy.Repositories;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _cs;
    public MySqlConnectionFactory(string cs) => _cs = cs;
    public IDbConnection CreateConnection() => new MySqlConnection(_cs);
}

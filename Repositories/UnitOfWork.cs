using MySqlConnector;
using System.Data;

namespace KhoQuanLy.Repositories;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    void Begin();
    void Commit();
    void Rollback();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _factory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
                _connection = _factory.CreateConnection();
            return _connection;
        }
    }

    public IDbTransaction Transaction
    {
        get
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction chưa được bắt đầu. Gọi Begin() trước.");
            return _transaction;
        }
    }

    public void Begin()
    {
        if (_connection == null)
            _connection = _factory.CreateConnection();
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
        _transaction = _connection.BeginTransaction();
    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            if (_connection != null && _connection.State == ConnectionState.Open)
                _connection.Close();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
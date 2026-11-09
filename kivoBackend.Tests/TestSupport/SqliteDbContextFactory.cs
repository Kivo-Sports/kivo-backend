using kivoBackend.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace kivoBackend.Tests.TestSupport;

/// <summary>
/// Real EF Core against a private, in-memory SQLite connection — used only for
/// integration tests that need actual query/Include/unique-index behavior
/// (RepositoryX classes), never for unit tests of services (those use the
/// hand-rolled in-memory fakes above). SQL Server is the real provider in
/// production; SQLite is close enough for the relational behavior exercised
/// here (see KIVO_BACKEND_TEST_COVERAGE_REPORT.md for the one known gap).
///
/// The SqliteConnection must stay open for the lifetime of the context (an
/// in-memory SQLite database is destroyed when its last connection closes),
/// so callers must dispose the returned handle after the test.
/// </summary>
public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteDbContextFactory(SqliteConnection connection)
    {
        _connection = connection;
    }

    public static SqliteDbContextFactory Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return new SqliteDbContextFactory(connection);
    }

    /// <param name="logSql">Opcional: recebe o SQL de cada comando, para testes que verificam a tradução da consulta.</param>
    public AppDbContext NewContext(Action<string>? logSql = null)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection);
        if (logSql != null)
            builder.LogTo(logSql, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information);
        var options = builder.Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}

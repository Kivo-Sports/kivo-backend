using System.Data;
using kivoBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Application.Services;

public static class UsuarioGestaoTransaction
{
    public static async Task<T> Executar<T>(AppDbContext db, Func<Task<T>> operacao)
    {
        if (db.Database.CurrentTransaction != null) return await operacao();
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // One ordering for all account/ownership writers prevents role/assignment races.
                if (db.Database.IsSqlServer())
                    await db.Usuarios.FromSqlRaw("SELECT * FROM [Usuarios] WITH (UPDLOCK, HOLDLOCK)").AsNoTracking().ToListAsync();
                else
                    await db.Usuarios.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
                var result = await operacao();
                await tx.CommitAsync();
                return result;
            }
            catch
            {
                await tx.RollbackAsync();
                db.ChangeTracker.Clear();
                throw;
            }
        });
    }
}

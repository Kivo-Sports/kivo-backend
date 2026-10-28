using System.Data;
using kivoBackend.Application.Services;
using kivoBackend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Presentation.Auth;

// Only used on team/championship writers that can assign or reactivate responsibilities.
public sealed class UsuarioResponsabilidadeFilter(AppDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method)) { await next(); return; }
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // Never replay an MVC action (uploads may have side effects). A database conflict is retryable by the client.
            ActionExecutedContext? executed = null;
            try
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                if (db.Database.IsSqlServer())
                    await db.Usuarios.FromSqlRaw("SELECT * FROM [Usuarios] WITH (UPDLOCK, HOLDLOCK)").AsNoTracking().ToListAsync();
                else await db.Usuarios.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
                if (!await UsuarioSessionValidation.Valida(db, context.HttpContext.User))
                {
                    context.Result = new UnauthorizedObjectResult(new { code = "session_invalid", message = "Sua sessão mudou. Entre novamente." });
                    return;
                }
                executed = await next();
                var status = (executed.Result as ObjectResult)?.StatusCode ?? (executed.Result as StatusCodeResult)?.StatusCode ?? 200;
                if (executed.Exception == null && status < 400) await tx.CommitAsync();
                else { await tx.RollbackAsync(); db.ChangeTracker.Clear(); }
            }
            catch (Exception ex) when (ex is System.Data.Common.DbException or DbUpdateException)
            {
                db.ChangeTracker.Clear();
                var conflict = new ConflictObjectResult(new { code = "concurrent_change", message = "Os dados foram alterados simultaneamente. Atualize e tente novamente." });
                if (executed != null) { executed.Result = conflict; executed.Exception = null; }
                else context.Result = conflict;
            }
        });
    }
}

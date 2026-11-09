namespace kivoBackend.Application.Interfaces;

public interface IUsuarioGestaoTransacao
{
    Task<T> Executar<T>(Func<Task<T>> operacao);
}

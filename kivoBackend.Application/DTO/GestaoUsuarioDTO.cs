using System.ComponentModel.DataAnnotations;
using kivoBackend.Core.Enums;

namespace kivoBackend.Application.DTO;

public class GestaoUsuarioFiltro
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [StringLength(200)] public string? Busca { get; set; }
    public EnumCargo? Cargo { get; set; }
    public string? Situacao { get; set; }
}
public record GestaoUsuarioItem(Guid Id, string Nome, string Email, string Cargo, string Situacao,
    bool PossuiEndereco, bool PossuiContaBanco);
public record GestaoUsuarioPagina(IEnumerable<GestaoUsuarioItem> Items, int Total, int Page, int PageSize);
public class GestaoUsuarioAcao { [Required] public string Acao { get; set; } = ""; }
public class AlterarCargoUsuarioDTO
{
    [EnumDataType(typeof(EnumCargo))] public EnumCargo Cargo { get; set; }
    public DadosPerfilGestaoDTO? DadosPerfil { get; set; }
}
public class DadosPerfilGestaoDTO
{
    public EnderecoDto? Endereco { get; set; }
    public ContaBancoDTO? ContaBanco { get; set; }
}
public class GestaoUsuarioException(string code, string message, object? impedimentos = null) : InvalidOperationException(message)
{
    public string Code { get; } = code;
    public object? Impedimentos { get; } = impedimentos;
}

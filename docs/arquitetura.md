# Arquitetura do backend

O backend é uma solução .NET 8 em quatro projetos. A regra central é a **regra de dependência**: as dependências apontam para dentro, em direção ao domínio, e nunca no sentido contrário.

## Camadas e regra de dependência

```
Core            ← (nada)
Application     → Core
Infrastructure  → Application, Core
Presentation    → Application, Infrastructure, Core   (composition root)
```

| Projeto | Responsabilidade |
|---|---|
| `kivoBackend.Core` | Entidades, enums e contratos de repositório. Não conhece nenhum outro projeto. |
| `kivoBackend.Application` | Regras de negócio (serviços), DTOs e as interfaces que a infraestrutura precisa implementar. |
| `kivoBackend.Infrastructure` | EF Core, migrations, repositórios, transação e validação de sessão. |
| `kivoBackend.Presentation` | Controllers, autenticação, Swagger e o registro de dependências em `kivoBackend.Presentation/Program.cs`. |

A Application declara o que precisa por interface e a Infrastructure fornece a implementação. A Presentation liga as duas no `Program.cs`. Por isso a Application pode ser testada sem SQL Server e a troca de tecnologia de dados não toca nas regras de negócio.

**Como a regra é provada:** `kivoBackend.Tests/Architecture/DependencyRuleTests.cs` lê os assemblies referenciados e falha se a Application referenciar Infrastructure ou Presentation, se o Core referenciar outro projeto Kivo ou se um controller injetar repositório ou `AppDbContext`.

```
dotnet test --filter DependencyRuleTests
```

## Padrões adotados

### Repository

- **Onde:** contratos em `kivoBackend.Core/Interfaces/IRepositoryGenerics.cs`, `IRepositoryUsuario.cs`, `IRepositoryTime.cs`, `IRepositoryCampeonato.cs`; implementações em `kivoBackend.Infrastructure/Repositories/RepositoryGenerics.cs`, `RepositoryUsuario.cs` e demais.
- **Por quê:** o acesso a dados fica atrás de uma interface. O CRUD genérico evita repetir código e os repositórios específicos guardam consultas com `Include` que só fazem sentido para uma entidade.

### Unit of Work

- **Onde:** `kivoBackend.Infrastructure/Data/AppDbContext.cs`. O `DbContext` do EF Core rastreia as mudanças e grava tudo em `SaveChangesAsync`. Para operações que precisam de isolamento forte, `kivoBackend.Infrastructure/Transactions/UsuarioGestaoTransacao.cs` abre uma transação serializável.
- **Por quê:** o EF Core já implementa o padrão, então não criamos uma camada própria por cima.

### Service Layer

- **Onde:** interfaces em `kivoBackend.Application/Interfaces/` (por exemplo `ITimeService.cs`) e implementações em `kivoBackend.Application/Services/` (por exemplo `TimeService.cs`). A base `ServiceGenerics.cs` concentra o CRUD comum.
- **Por quê:** as regras de negócio ficam em um lugar só. O controller recebe a requisição, chama o serviço e devolve a resposta. Um exemplo é `TimeService.ParticipaDeCampeonatoAtivo`, que antes estava escrita no controller.

### DTO

- **Onde:** `kivoBackend.Application/DTO/`, por exemplo `OrganizadorResumoDTO.cs` e `CriarTimeDTO.cs`.
- **Por quê:** a API expõe um contrato próprio e estável, sem vazar entidades do banco nem campos internos.

### Dependency Injection

- **Onde:** `kivoBackend.Presentation/Program.cs` registra tudo, inclusive `IAppDbContext` e `IUsuarioGestaoTransacao`. Os serviços recebem suas dependências pelo construtor.
- **Por quê:** é o que permite inverter a dependência e trocar implementações por fakes nos testes (`kivoBackend.Tests/TestSupport/Fakes.cs`).

### Inversão de dependência para dados (`IAppDbContext`)

- **Onde:** `kivoBackend.Application/Interfaces/IAppDbContext.cs`, implementada por `AppDbContext`. É usada por `RelatorioService.cs` e `UsuarioService.cs`.
- **Por quê:** a Application consulta com LINQ sem conhecer o banco. A interface expõe só os `DbSet<>` que a Application usa e `SaveChangesAsync`.

### Query Service

- **Onde:** `kivoBackend.Application/Services/RelatorioService.cs`.
- **Por quê:** os relatórios só leem. Eles consultam e agrupam direto pelo contexto, sem passar por repositórios de escrita, e as agregações rodam no banco.

### Adapter / Strategy para integrações externas

- **Onde:** `kivoBackend.Application/Interfaces/IAsaasService.cs`, `IStorageService.cs` e `IEmailService.cs`, com implementações em `AsaasService.cs`, `ImageStorageService.cs` e `EmailService.cs`.
- **Por quê:** pagamento, armazenamento de imagens e e-mail são serviços de terceiros. O resto do sistema depende da interface, então trocar o provedor afeta uma classe.

## SOLID, com exemplos

- **Responsabilidade única:** `kivoBackend.Application/Auth/SessaoClaims.cs` só guarda o nome da claim. `kivoBackend.Infrastructure/Auth/UsuarioSessionValidation.cs` só valida a sessão contra o banco.
- **Aberto/fechado:** novas integrações entram como nova implementação de `IStorageService` ou `IEmailService`, sem mudar quem as usa.
- **Substituição de Liskov:** os serviços derivam de `ServiceGenerics.cs` e respeitam o contrato de `IServiceGenerics.cs`. `kivoBackend.Tests/TestSupport/Fakes.cs` substitui as implementações reais nos testes.
- **Segregação de interfaces:** `IAppDbContext.cs` expõe só o que a Application usa, e `IUsuarioGestaoTransacao.cs` tem um único método.
- **Inversão de dependência:** a Application depende de `IAppDbContext` e `IUsuarioGestaoTransacao`, e a Infrastructure os implementa.

## Trade-offs assumidos

- **EF Core na Application.** `IAppDbContext` devolve `DbSet<>`, então a Application referencia o pacote `Microsoft.EntityFrameworkCore` (só as abstrações de consulta, sem provedor de banco). Aceitamos isso para manter o mesmo LINQ nos serviços e evitar uma camada de repositórios para cada relatório. Em troca, a Application não referencia a Infrastructure nem o SQL Server.
- **Transação e hint de lock na Infrastructure.** O `WITH (UPDLOCK, HOLDLOCK)` do SQL Server fica em `UsuarioGestaoTransacao.cs`, e a Application conhece só `IUsuarioGestaoTransacao`.
- **Validação de sessão na Infrastructure.** Ela depende das tabelas do Identity, então fica junto do acesso a dados. A Presentation a chama em `Program.cs` e em `UsuarioResponsabilidadeFilter.cs`.

## Dívida técnica conhecida

Alguns serviços cresceram e acumulam responsabilidades. Quebrá-los em serviços menores fica para uma próxima etapa, sem mudar comportamento:

- `kivoBackend.Application/Services/PartidaService.cs` (cerca de 700 linhas);
- `kivoBackend.Application/Services/IngressoService.cs` (cerca de 580 linhas);
- `kivoBackend.Application/Services/UsuarioService.cs` e `UsuarioService.Gestao.cs` (cerca de 760 linhas somadas).

<div align="center">

<img src="https://img.shields.io/badge/version-0.1.0-00C896?style=for-the-badge" />
<img src="https://img.shields.io/badge/status-em%20desenvolvimento-FFB800?style=for-the-badge" />

# ⚽ KIVO SPORTS — Backend

### Plataforma web de gestão de campeonatos esportivos e venda de ingressos digitais para eventos amadores e semiprofissionais.

[Sobre](#-sobre-o-projeto) · [Stack](#-stack) · [Estrutura](#-estrutura-do-projeto) · [Como rodar](#-como-rodar) · [Variáveis de ambiente](#-variáveis-de-ambiente) · [Relatórios](#-relatórios-e-métricas) · [Pagamentos & Asaas](#-pagamentos--asaas-pix) · [Testes & Cobertura](#-testes--cobertura) · [Padrões](#-padrões-de-desenvolvimento)

</div>

---

## 📌 Sobre o Projeto

O **Kivo Sports** é uma plataforma web que digitaliza e moderniza a gestão de eventos esportivos de pequeno e médio porte. Organizadores criam campeonatos, gerenciam times e jogos, registram resultados e publicam notícias — enquanto torcedores acompanham tabelas, resultados e compram ingressos digitais via PIX.

Este repositório contém o **backend** da plataforma, desenvolvido em **.NET 8** com arquitetura em camadas.

---

## 🧩 Stack

| Camada                 | Tecnologia                                                            |
| ---------------------- | ----------------------------------------------------------------------- |
| Framework              | [.NET 8](https://dotnet.microsoft.com/)                                 |
| Linguagem              | [C#](https://docs.microsoft.com/en-us/dotnet/csharp/)                   |
| Banco de Dados         | [SQL Server](https://www.microsoft.com/sql-server/) (LocalDB)           |
| ORM                    | [Entity Framework Core 8](https://docs.microsoft.com/ef/core/)          |
| Autenticação           | [ASP.NET Core Identity](https://docs.microsoft.com/aspnet/identity/)    |
| Pagamentos (Pix)       | [Asaas API](https://www.asaas.com/) (Sandbox/Homologação)               |
| Documentação API       | [Swagger/OpenAPI](https://swagger.io/)                                  |
| Injeção de Dependência | ASP.NET Core nativa                                                     |

---

## 🗂 Estrutura do Projeto

O projeto segue a arquitetura **Clean Architecture** em 4 camadas:

```
kivoBackend/
├── kivoBackend.Presentation/          # Controllers e configuração da API
│   ├── Program.cs                      # Configuração da aplicação
│   ├── appsettings.json                # Configurações de produção
│   ├── appsettings.Development.json    # Configurações de desenvolvimento
│   ├── kivoBackend.Presentation.http   # Testes HTTP via REST Client
│   └── Controllers/                    # Endpoints da API
│
├── kivoBackend.Application/            # Business logic e DTOs
│   ├── Services/                       # Implementação de serviços
│   ├── Interfaces/                     # Contratos de serviços
│   └── DTO/                            # Data Transfer Objects
│
├── kivoBackend.Core/                   # Entidades e regras de negócio
│   ├── Entities/                       # Modelos de domínio
│   ├── Enums/                          # Enumerações (Cargo, Status...)
│   └── Interfaces/                     # Contratos de repositório
│
├── kivoBackend.Infrastructure/         # Acesso a dados e repositórios
│   ├── Data/                           # DbContext e configuração
│   ├── Repositories/                   # Implementação de repositórios
│   └── Migrations/                     # Histórico de alterações no BD
│
└── kivoBackend.sln                     # Solution Visual Studio
```

### Entidades Principais

- **Usuario** → Usuário da plataforma (pode ser Torcedor, Organizador ou Administrador)
- **Campeonato** → Torneio esportivo
- **Time** → Equipe participante
- **CampeonatoTime** → Relacionamento entre Campeonato e Time
- **Torcedor** → Usuário que acompanha campeonatos
- **OrganizadorCampeonato** → Responsável pela criação e gestão do campeonato
- **OrganizadorTime** → Responsável pela gestão do time
- **ContaBanco** → Dados bancários para recebimento de ingressos
- **Endereco** → Endereço do usuário ou evento
- **Esporte** → Modalidade esportiva dos campeonatos e times
- **Partida** → Jogos, placares, agenda, tabela e chaveamento
- **IngressoLote** e **Ingresso** → Estoque, compra Pix, titularidade e validação na portaria
- **Favorito** → Times e campeonatos acompanhados pelo torcedor
- **Notificacao** → Avisos de pagamento, ingresso e atividades da plataforma
- **Post** → Notícias e publicações de times e campeonatos

---

## 🚀 Como Rodar

### Pré-requisitos

- .NET 8 SDK
- SQL Server (LocalDB ou instância)
- Visual Studio com suporte ao .NET 8 ou VS Code

### Instalação

```bash
# Clone o repositório
git clone https://github.com/kivo-sports/kivo-backend.git
cd kivo-backend

# Restaure as dependências
dotnet restore

# Aplique as migrações do banco de dados
dotnet ef database update --project kivoBackend.Infrastructure --startup-project kivoBackend.Presentation

# Execute o servidor
dotnet run --project kivoBackend.Presentation
```

Acesse a documentação da API em `http://localhost:5211/swagger` (ou `https://localhost:7273/swagger`). O Swagger é disponibilizado somente no ambiente `Development`.

---

## 🔑 Variáveis de Ambiente

### Configuração com `.env`

Para manter informações sensíveis fora do controle de versão, o projeto usa um arquivo `.env`:

**Pré-requisito:**
- Biblioteca `DotNetEnv` (já instalada)

### Primeira Execução

1. Copie o arquivo template:
```bash
cp .env.example .env
```

2. Edite o arquivo `.env` com seus valores reais:
```env
# Database
DB_CONNECTION_STRING=Server=localhost\SQLEXPRESS;Database=KivoDb;Trusted_Connection=True;TrustServerCertificate=True;

# JWT
JWT_KEY=sua_chave_super_secreta_aqui
JWT_ISSUER=kivoBackend
JWT_AUDIENCE=kivoFrontEnd

# Email (Gmail App Password)
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SENDER_EMAIL=seu_email@gmail.com
SENDER_PASSWORD=xxxx xxxx xxxx xxxx
SENDER_NAME=Kivo Sports
ENABLE_SSL=true

# Asaas (Pagamentos Pix - Sandbox)
ASAAS_API_KEY=sua_api_key_sandbox_aqui
ASAAS_BASE_URL=https://sandbox.asaas.com/api/v3

# Armazenamento de imagens (Firebase)
FIREBASE_BUCKET=
GOOGLE_APPLICATION_CREDENTIALS=

# Frontend CORS (separados por vírgula)
CORS_ORIGINS=http://localhost:3000,http://localhost:3001

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

### Variáveis Disponíveis

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `DB_CONNECTION_STRING` | Conexão SQL Server | `Server=localhost\SQLEXPRESS;Database=KivoDb;...` |
| `JWT_KEY` | Chave secreta para assinar tokens | Mínimo 32 caracteres |
| `JWT_ISSUER` | Emissor do token JWT | `kivoBackend` |
| `JWT_AUDIENCE` | Audiência do token JWT | `kivoFrontEnd` |
| `SMTP_SERVER` | Servidor SMTP | `smtp.gmail.com` |
| `SMTP_PORT` | Porta SMTP | `587` |
| `SENDER_EMAIL` | Email para enviar códigos e notificações | `seu_email@gmail.com` |
| `SENDER_PASSWORD` | App Password (não a senha da conta) | `xxxx xxxx xxxx xxxx` |
| `SENDER_NAME` | Nome do remetente de emails | `Kivo Sports` |
| `ENABLE_SSL` | Usar SSL no SMTP | `true` |
| `ASAAS_API_KEY` | Chave de API do Asaas (Sandbox) | `$aact_YourApiKeyHere` |
| `ASAAS_BASE_URL` | URL base da API do Asaas | `https://sandbox.asaas.com/api/v3` |
| `FIREBASE_BUCKET` | Bucket usado no armazenamento de imagens | `kivo-sports.firebasestorage.app` |
| `GOOGLE_APPLICATION_CREDENTIALS` | Caminho das credenciais de serviço do Firebase | `/caminho/credenciais.json` |
| `CORS_ORIGINS` | Origens permitidas (separadas por vírgula) | `http://localhost:3000,http://localhost:3001` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | `Development` ou `Production` |

### 🔒 Segurança

- ✅ `.env` está no `.gitignore` — **nunca será commitado**
- ✅ Cada desenvolvedor tem seu próprio `.env` local
- ✅ Em produção, configure variáveis de ambiente do servidor (Azure App Service, Docker, VPS, etc)
- ✅ `.env.example` é compartilhado com a equipe (sem valores sensíveis)

### 📧 Configurar Email (Gmail)

1. Ative 2FA na sua conta Google
2. Gere um **App Password** em [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords)
3. Use o App Password no `.env`:
```env
SENDER_PASSWORD=xxxx xxxx xxxx xxxx
```

### 🏭 Em Produção

Ao fazer deploy (Azure, Heroku, VPS), configure as variáveis de ambiente no servidor:

**Azure App Service:**
```bash
az webapp config appsettings set --resource-group MyGroup --name MyApp \
  --settings ASPNETCORE_ENVIRONMENT=Production \
  DB_CONNECTION_STRING="..." \
  JWT_KEY="..." \
  SENDER_PASSWORD="..." \
  ASAAS_API_KEY="..."
```

**Docker:**
```dockerfile
ENV DB_CONNECTION_STRING="..."
ENV JWT_KEY="..."
ENV ASAAS_API_KEY="..."
```

**Linux/VPS:**
```bash
export DB_CONNECTION_STRING="..."
export JWT_KEY="..."
export ASAAS_API_KEY="..."
dotnet run
```

---

## Relatórios e Métricas

Os endpoints de relatórios retornam dados prontos para o frontend montar gráficos e cards. Todos exigem autenticação JWT e aceitam `Inicio` e `Fim` como filtros opcionais de período; o relatório administrativo também aceita `EsporteId`.

| Perfil | Endpoint | Principais dados retornados |
| --- | --- | --- |
| Organizador de campeonato | `GET /api/relatorios/organizador-campeonato` | Receita, vendas por dia e partida, ocupação de lotes, campeonatos, times e partidas |
| Organizador de time | `GET /api/relatorios/organizador-time` | Desempenho, jogos, vitórias, gols, campeonatos e favoritos dos times administrados |
| Torcedor | `GET /api/relatorios/torcedor` | Gastos, ingressos, favoritos, compras por dia e próximas partidas |
| Administrador | `GET /api/relatorios/administrativo` | Usuários, campeonatos, partidas, times, receita, vendas e distribuições por perfil e esporte |

Exemplo de filtro: `GET /api/relatorios/organizador-campeonato?Inicio=2026-01-01&Fim=2026-12-31`.

---

## 💳 Pagamentos & Asaas (Pix)

A plataforma utiliza a API do **Asaas** em ambiente **Sandbox (Homologação)** para simular o fluxo financeiro completo, autônomo e em tempo real de compra de ingressos.

### 🔄 Fluxo de Compra e Validação

```text
[Torcedor] ── Solicita Compra ──► [API Kivo] ── Cria Cobrança Pix ──► [Asaas Sandbox]
                                        │                                    │
                               Retorna Pix Copia e Cola                      │
                               e QR Code de Pagamento                        │
                                        │                                    │
[Torcedor] ◄────────────────────────────┘                                    │
    │                                                                        │
    └── Simula Pagamento no Sandbox ─────────────────────────────────────────┘
                                                                             │
                                              Asaas dispara Webhook          │
                                              com status PAYMENT_RECEIVED    │
                                                                             │
[Portaria / Evento] ◄── Libera Ingresso ◄── Atualiza Banco (Status: Pago) ◄──┘
 (Valida QR Code)
```

**Geração do Pix**
O torcedor escolhe um ou mais lotes, com máximo de 10 ingressos por compra. A API cria/vincula o cliente no Asaas e gera uma cobrança Pix para todo o carrinho, com um identificador único da compra em `externalReference`, retornando o código Copia e Cola e a imagem do QR Code em Base64. Os ingressos iniciam com status **Pendente (0)**.

**Confirmação via Webhook**
Ao efetuar ou simular o pagamento no Asaas Sandbox, os servidores do Asaas disparam uma notificação HTTP `POST` (`PAYMENT_RECEIVED` ou `PAYMENT_CONFIRMED`) para a rota `/api/webhook/asaas`.

**Liberação do Ingresso**
O backend processa o webhook de forma assíncrona e atualiza o status no banco de dados para **Pago (1)**. Como alternativa de conferência, o comprador pode chamar `POST /api/Ingresso/pagar/{ingressoId}`, que consulta o status da cobrança no Asaas antes de confirmar a compra.

**Titularidade e validação na portaria**
Depois do pagamento, o comprador atribui o titular pelo endpoint `PUT /api/Ingresso/{ingressoId}/titular`. Na entrada do evento, o aplicativo do organizador lê o código de validação e consome `POST /api/Ingresso/validar-portaria/{codigo}`, alterando o status para **Utilizado (3)** e impedindo tentativas de reutilização. Pagamento confirmado e titular atribuído são obrigatórios para liberar a entrada.

### 🌐 Como Testar o Webhook em Desenvolvimento Local (Túnel Cloudflare)

Como o Asaas precisa notificar uma URL pública na internet, utilizamos o túnel da Cloudflare para encaminhar as requisições até o `localhost:5211`:

1. Inicie a API no Visual Studio (garantindo que suba na porta `5211`).
2. Abra o terminal e execute o túnel:
```powershell
cloudflared tunnel --url http://localhost:5211
```
3. Copie a URL pública gerada (exemplo: `https://nome-aleatorio.trycloudflare.com`).
4. Cadastre no Painel do Asaas:
   - Acesse `sandbox.asaas.com` → **Integrações** → **Webhooks**.
   - No campo **URL do Webhook**, preencha com o endpoint completo:
     ```
     https://nome-aleatorio.trycloudflare.com/api/webhook/asaas
     ```
   - Marque a opção para receber eventos de cobrança (`PAYMENT_RECEIVED`, `PAYMENT_CONFIRMED`) e salve.

---

### Estorno de cobrança Pix

Uma compra paga pode ser estornada pelo próprio comprador ou por um administrador. O estorno é sempre aplicado à cobrança inteira do Asaas; portanto, se a compra tiver mais de um ingresso, todos os ingressos vinculados à mesma cobrança são incluídos.

#### Endpoint

```http
POST /api/Ingresso/{ingressoId}/estorno
Authorization: Bearer {token}
Content-Type: application/json

{
  "motivo": "Não poderei comparecer ao evento"
}
```

O campo `motivo` é opcional. Em caso de solicitação aceita, a API responde `202 Accepted`.

#### Regras e comportamento

- Somente o comprador ou um administrador pode solicitar o estorno.
- A compra precisa estar integralmente paga; ingressos já utilizados na portaria não podem ser estornados.
- Logo após a solicitação, os ingressos ficam com status `EstornoPendente` e deixam de aparecer em `GET /api/Ingresso/meus-ingressos`. O front pode atualizar a lista após receber o `202`.
- Quando o Asaas confirmar o evento `PAYMENT_REFUNDED` no webhook, os ingressos são cancelados e as unidades retornam ao lote, respeitando sua quantidade total.
- Se o Asaas enviar `PAYMENT_REFUND_DENIED`, os ingressos voltam ao status `Pago` e reaparecem para o torcedor. Nesse caso, nenhuma vaga é devolvida ao lote.

> A confirmação financeira é responsabilidade do Asaas. A API não informa que o dinheiro foi devolvido antes de receber `PAYMENT_REFUNDED`.

#### Teste no Sandbox

1. Faça uma compra Pix e confirme o pagamento no Asaas, deixando o ingresso como `Pago`.
2. Chame o endpoint de estorno com o ID de um dos ingressos da compra e um token do comprador ou administrador.
3. Confira que o ingresso saiu de `GET /api/Ingresso/meus-ingressos`.
4. No painel do Asaas, acompanhe o estorno e mantenha o webhook configurado para receber `PAYMENT_REFUND_IN_PROGRESS`, `PAYMENT_REFUNDED` e `PAYMENT_REFUND_DENIED` em `/api/webhook/asaas`.

Para simular somente a confirmação do provedor durante uma apresentação, envie este payload ao webhook local **enquanto a compra estiver em `EstornoPendente`**:

```json
{
  "event": "PAYMENT_REFUNDED",
  "payment": { "id": "pay_id_da_cobranca" }
}
```

Isso valida o fluxo interno (cancelamento e devolução da vaga), mas não realiza uma transferência financeira real. Para testar o Pix completo no Sandbox, use uma segunda conta Sandbox do Asaas como pagadora da cobrança original.

---

## 🧪 Testes & Cobertura

O projeto usa **xUnit** para a suíte de testes (`kivoBackend.Tests`), com testes unitários
(`Unit/`, fakes em `TestSupport/`) e testes de integração de repositório contra SQLite
in-memory (`Integration/`).

### Rodar os testes

```bash
# Rodar toda a suíte
dotnet test

# Rodar só uma classe/namespace (filtro por nome do teste)
dotnet test --filter "FullyQualifiedName~UsuarioServiceTests"

# Ver cada teste sendo executado (saída detalhada)
dotnet test --logger "console;verbosity=detailed"
```

### Gerar o relatório de cobertura

A cobertura é coletada com o **coverlet.collector** (já referenciado no projeto de testes) e
as regras de exclusão (Migrations, Program.cs, DTOs, Interfaces — código declarativo/gerado,
sem comportamento) ficam em [`coverlet.runsettings`](./coverlet.runsettings).

```bash
# 1. Instale o reportgenerator (uma vez só, globalmente)
dotnet tool install -g dotnet-reportgenerator-globaltool
# garanta que ~/.dotnet/tools está no PATH:
export PATH="$PATH:$HOME/.dotnet/tools"

# 2. Rode os testes coletando cobertura
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# 3. Gere o relatório HTML + resumo em texto a partir do .cobertura.xml gerado
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html;TextSummary"

# 4. Veja o resumo no terminal...
cat coverage-report/Summary.txt

# ...ou abra o relatório navegável no browser
open coverage-report/index.html   # macOS
# xdg-open coverage-report/index.html   # Linux
```

Para começar do zero (útil depois de mudanças grandes na suíte, evita misturar resultados de
execuções antigas):

```bash
find . -type d -name TestResults -prune -exec rm -rf {} +
rm -rf coverage-report
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;TextSummary"
```

O estado atual da cobertura (baseline vs. atual, por área, gaps conhecidos e por quê) está
documentado em [`KIVO_BACKEND_TEST_COVERAGE_REPORT.md`](./KIVO_BACKEND_TEST_COVERAGE_REPORT.md).

---

## Documentação

> 📚 Documentação completa disponível em `/swagger` após iniciar a aplicação

---

## 🎨 Padrões de Desenvolvimento

### Clean Architecture

- **Presentation** → Apenas Controllers (exposição de endpoints)
- **Application** → DTOs e Serviços (lógica de aplicação)
- **Core** → Entidades e Interfaces (regras de negócio)
- **Infrastructure** → Repositórios e DbContext (persistência)

### Commits

Seguimos o padrão **Conventional Commits**:

```
feat: adiciona endpoint de listagem de campeonatos
fix: corrige validação de email duplicado
refactor: reorganiza estrutura de repositórios
chore: atualiza Entity Framework
```

### Branches

```
main          → produção
develop       → desenvolvimento
feat/nome     → novas funcionalidades
fix/nome      → correções
```

---

## 🔗 Repositórios Relacionados

| Repositório                                                     | Descrição              |
| --------------------------------------------------------------- | ---------------------- |
| [kivo-frontend](https://github.com/kivo-sports/kivo-frontend)   | Frontend em Next.js    |

---

## 🔌 Migrations (Entity Framework)

### Criar uma nova migration

```bash
dotnet ef migrations add NomeDaMigracao --project kivoBackend.Infrastructure --startup-project kivoBackend.Presentation
```

### Aplicar migrations

```bash
dotnet ef database update --project kivoBackend.Infrastructure --startup-project kivoBackend.Presentation
```

### Remover última migration

```bash
dotnet ef migrations remove --project kivoBackend.Infrastructure --startup-project kivoBackend.Presentation
```

---

## 👥 Time

Desenvolvido pelo time **Kivo Sports**.

---

<div align="center">
  <sub>Kivo Sports © 2025 — Digitalizando o esporte amador brasileiro</sub>
</div>

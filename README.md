<div align="center">

<img src="https://img.shields.io/badge/version-0.1.0-00C896?style=for-the-badge" />
<img src="https://img.shields.io/badge/status-em%20desenvolvimento-FFB800?style=for-the-badge" />

# ⚽ KIVO SPORTS — Backend

### Plataforma web de gestão de campeonatos esportivos e venda de ingressos digitais para eventos amadores e semiprofissionais.

[Sobre](#-sobre-o-projeto) · [Stack](#-stack) · [Estrutura](#-estrutura-do-projeto) · [Como rodar](#-como-rodar) · [Variáveis de ambiente](#-variáveis-de-ambiente) · [Pagamentos & Asaas](#-pagamentos--asaas-pix) · [Padrões](#-padrões-de-desenvolvimento)

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

---

## 🚀 Como Rodar

### Pré-requisitos

- .NET 8 SDK
- SQL Server (LocalDB ou instância)
- Visual Studio 2023 ou VS Code

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

Acesse a documentação da API em [https://localhost:5001/swagger](https://localhost:5001/swagger)

---

## 🔑 Variáveis de Ambiente

### Configuração com `.env`

Para manter informações sensíveis fora do controle de versão, o projeto usa um arquivo `.env`:

**Pré-requisito:**
- Biblioteca `DotNetEnv` (já instalada)

### Primeira Execução

1. Copie o arquivo template:
```bash
cp kivoBackend.Presentation/.env.example kivoBackend.Presentation/.env
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
ASAAS_API_URL=https://sandbox.asaas.com/api/v3

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
| `ASAAS_API_URL` | URL base da API do Asaas | `https://sandbox.asaas.com/api/v3` |
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
O torcedor escolhe o lote e a quantidade. A API cria/vincula o cliente no Asaas e gera a cobrança Pix com `externalReference` vinculado ao ID do ingresso, retornando o código Copia e Cola e a imagem do QR Code em Base64 com status inicial **Pendente (0)**.

**Confirmação via Webhook**
Ao efetuar ou simular o pagamento no Asaas Sandbox, os servidores do Asaas disparam uma notificação HTTP `POST` (`PAYMENT_RECEIVED` ou `PAYMENT_CONFIRMED`) para a rota `/api/webhook/asaas`.

**Liberação do Ingresso**
O backend processa o webhook de forma assíncrona, atualiza o status no banco de dados para **Pago (1)** e gera o QR Code oficial de entrada baseado no código de validação único.

**Validação na Portaria**
Na entrada do evento, o aplicativo do organizador lê o QR Code e consome o endpoint `POST /api/ingresso/validar-portaria`, alterando o status para **Utilizado (2)** e impedindo tentativas de reutilização.

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

<div align="center">

<img src="https://img.shields.io/badge/version-0.1.0-00C896?style=for-the-badge" />
<img src="https://img.shields.io/badge/status-em%20desenvolvimento-FFB800?style=for-the-badge" />

# ⚽ KIVO SPORTS — Backend

### Plataforma web de gestão de campeonatos esportivos e venda de ingressos digitais para eventos amadores e semiprofissionais.

[Sobre](#-sobre-o-projeto) · [Stack](#-stack) · [Estrutura](#-estrutura-do-projeto) · [Como rodar](#-como-rodar) · [Variáveis de ambiente](#-variáveis-de-ambiente) · [API](#-endpoints-principais) · [Padrões](#-padrões-de-desenvolvimento)

</div>

---

## 📌 Sobre o Projeto

O **Kivo Sports** é uma plataforma web que digitaliza e moderniza a gestão de eventos esportivos de pequeno e médio porte. Organizadores criam campeonatos, gerenciam times e jogos, registram resultados e publicam notícias — enquanto torcedores acompanham tabelas, resultados e compram ingressos digitais via PIX.

Este repositório contém o **backend** da plataforma, desenvolvido em **.NET 8** com arquitetura em camadas.

---

## 🧩 Stack

| Camada             | Tecnologia                                                              |
| ------------------ | ----------------------------------------------------------------------- |
| Framework          | [.NET 8](https://dotnet.microsoft.com/)                                 |
| Linguagem          | [C#](https://docs.microsoft.com/en-us/dotnet/csharp/)                   |
| Banco de Dados     | [SQL Server](https://www.microsoft.com/sql-server/) (LocalDB)           |
| ORM                | [Entity Framework Core 8](https://docs.microsoft.com/ef/core/)           |
| Autenticação       | [ASP.NET Core Identity](https://docs.microsoft.com/aspnet/identity/)     |
| Documentação API   | [Swagger/OpenAPI](https://swagger.io/)                                  |
| Injeção de Dependência | ASP.NET Core nativa                                                  |

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
  SENDER_PASSWORD="..."
```

**Docker:**
```dockerfile
ENV DB_CONNECTION_STRING="..."
ENV JWT_KEY="..."
```

**Linux/VPS:**
```bash
export DB_CONNECTION_STRING="..."
export JWT_KEY="..."
dotnet run
```

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

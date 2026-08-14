using DotNetEnv;
using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using kivoBackend.Infrastructure.Data;
using kivoBackend.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

// 1. Leitura direta e sem falhas do arquivo .env
var dirAtual = new DirectoryInfo(Directory.GetCurrentDirectory());
string caminhoEnv = null;

while (dirAtual != null)
{
    var testeEnv = Path.Combine(dirAtual.FullName, ".env");
    if (File.Exists(testeEnv))
    {
        caminhoEnv = testeEnv;
        break;
    }
    dirAtual = dirAtual.Parent;
}

if (!string.IsNullOrEmpty(caminhoEnv))
{
    foreach (var linha in File.ReadAllLines(caminhoEnv))
    {
        var trimLinha = linha.Trim();
        if (string.IsNullOrWhiteSpace(trimLinha) || trimLinha.StartsWith("#")) continue;

        var partes = trimLinha.Split('=', 2);
        if (partes.Length == 2)
        {
            var chave = partes[0].Trim();
            var valor = partes[1].Trim().Trim('\'').Trim('"');
            Environment.SetEnvironmentVariable(chave, valor);
        }
    }
    Console.WriteLine($"=================================================");
    Console.WriteLine($"[.ENV CARREGADO COM SUCESSO DE]: {caminhoEnv}");
    Console.WriteLine($"[ASAAS KEY CONFIGURADA]: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASAAS_API_KEY"))}");
    Console.WriteLine($"=================================================");
}

var builder = WebApplication.CreateBuilder(args);

// 2. Adicionar appsettings e variáveis de ambiente ao Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var config = builder.Configuration;

// 3. Overrides condicionais (não sobrescreve se a variável for vazia)
var dbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(dbConnection))
    builder.Configuration["ConnectionStrings:DefaultConnection"] = dbConnection;

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (!string.IsNullOrWhiteSpace(jwtKey))
    builder.Configuration["Jwt:Key"] = jwtKey;

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
if (!string.IsNullOrWhiteSpace(jwtIssuer))
    builder.Configuration["Jwt:Issuer"] = jwtIssuer;

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
if (!string.IsNullOrWhiteSpace(jwtAudience))
    builder.Configuration["Jwt:Audience"] = jwtAudience;

// Overrides de Email
var smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER");
if (!string.IsNullOrWhiteSpace(smtpServer)) builder.Configuration["EmailSettings:SmtpServer"] = smtpServer;

var smtpPort = Environment.GetEnvironmentVariable("SMTP_PORT");
if (!string.IsNullOrWhiteSpace(smtpPort)) builder.Configuration["EmailSettings:SmtpPort"] = smtpPort;

var senderEmail = Environment.GetEnvironmentVariable("SENDER_EMAIL");
if (!string.IsNullOrWhiteSpace(senderEmail)) builder.Configuration["EmailSettings:SenderEmail"] = senderEmail;

var senderPassword = Environment.GetEnvironmentVariable("SENDER_PASSWORD");
if (!string.IsNullOrWhiteSpace(senderPassword)) builder.Configuration["EmailSettings:SenderPassword"] = senderPassword;

var senderName = Environment.GetEnvironmentVariable("SENDER_NAME");
if (!string.IsNullOrWhiteSpace(senderName)) builder.Configuration["EmailSettings:SenderName"] = senderName;

var enableSSL = Environment.GetEnvironmentVariable("ENABLE_SSL");
if (!string.IsNullOrWhiteSpace(enableSSL)) builder.Configuration["EmailSettings:EnableSSL"] = enableSSL;

// Override de CORS e Firebase
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
if (!string.IsNullOrWhiteSpace(corsOrigins)) builder.Configuration["CORS_ORIGINS"] = corsOrigins;

builder.Configuration["FIREBASE_BUCKET"] = Environment.GetEnvironmentVariable("FIREBASE_BUCKET");
builder.Configuration["GOOGLE_APPLICATION_CREDENTIALS"] = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

// Override do ASAAS (Mantém o valor do appsettings.json caso o .env falhe)
var asaasApiKey = Environment.GetEnvironmentVariable("ASAAS_API_KEY");
if (!string.IsNullOrWhiteSpace(asaasApiKey))
    builder.Configuration["Asaas:ApiKey"] = asaasApiKey.Trim().Trim('"');

var asaasBaseUrl = Environment.GetEnvironmentVariable("ASAAS_BASE_URL");
if (!string.IsNullOrWhiteSpace(asaasBaseUrl))
    builder.Configuration["Asaas:BaseUrl"] = asaasBaseUrl.Trim().Trim('"');

// Controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration["CORS_ORIGINS"]?.Split(',') ?? new[] { "http://localhost:3000", "http://localhost:3001" };
    options.AddPolicy("AllowFrontend", corsBuilder =>
    {
        corsBuilder.WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\""
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(60);
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        }
    )
);

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// --- CONFIGURAÇÃO DE AUTENTICAÇÃO JWT ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"message\": \"Você precisa estar logado para acessar este recurso.\"}");
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"message\": \"Acesso negado: seu perfil não tem permissão para esta ação.\"}");
        }
    };
});

// Generic Repository / Service / Dependency Injection
builder.Services.AddScoped(typeof(IRepositoryGenerics<>), typeof(RepositoryGenerics<>));
builder.Services.AddScoped(typeof(IServiceGenerics<>), typeof(ServiceGenerics<>));
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IRepositoryUsuario, RepositoryUsuario>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<ITimeService, TimeService>();
builder.Services.AddScoped<ICampeonatoService, CampeonatoService>();
builder.Services.AddScoped<IPartidaService, PartidaService>();
builder.Services.AddScoped<IIngressoLoteService, IngressoLoteService>();
builder.Services.AddScoped<IIngressoService, IngressoService>();
builder.Services.AddScoped<IRepositoryCampeonato, RepositoryCampeonato>();
builder.Services.AddScoped<IRepositoryTime, RepositoryTime>();
builder.Services.AddScoped<IStorageService, ImageStorageService>();
builder.Services.AddScoped<IFavoritoService, FavoritoService>();

// Registra HttpClient com AsaasService
builder.Services.AddHttpClient<IAsaasService, AsaasService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Inicialização de Roles e Usuário Admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var usuarioRepository = scope.ServiceProvider.GetRequiredService<IRepositoryGenerics<Usuario>>();

    var roles = new[] { "Administrador", "Torcedor", "OrganizadorTime", "OrganizadorCampeonato" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@kivo.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var newAdmin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newAdmin, "Admin@123456");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin, "Administrador");

            var usuarioAdmin = new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Administrador",
                Email = adminEmail,
                Cpf = "00000000000",
                Telefone = "",
                DataNascimento = new DateTime(2000, 1, 1),
                EnumCargo = EnumCargo.Administrador,
                Ativo = true,
                CriadoEm = DateTime.Now
            };

            await usuarioRepository.Adicionar(usuarioAdmin);
        }
    }
}

app.Run();
using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Infrastructure.Data;
using kivoBackend.Infrastructure.Repositories;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Carregar variáveis de ambiente do arquivo .env
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Adicionar variáveis de ambiente ao Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Substituir placeholders de variáveis de ambiente no appsettings
var config = builder.Configuration;
var dbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Server=localhost\\SQLEXPRESS;Database=KivoDb;Trusted_Connection=True;TrustServerCertificate=True;";
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "KivoSports_Chave_Super_Secreta_2026_@!";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "kivoBackend";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "kivoFrontEnd";

// Override da connection string
builder.Configuration["ConnectionStrings:DefaultConnection"] = dbConnection;
builder.Configuration["Jwt:Key"] = jwtKey;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;

// Override das configurações de Email
var smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? "smtp.gmail.com";
var smtpPort = Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
var senderEmail = Environment.GetEnvironmentVariable("SENDER_EMAIL") ?? "kivosportsuporte@gmail.com";
var senderPassword = Environment.GetEnvironmentVariable("SENDER_PASSWORD") ?? "";
var senderName = Environment.GetEnvironmentVariable("SENDER_NAME") ?? "Kivo Sports";
var enableSSL = Environment.GetEnvironmentVariable("ENABLE_SSL") ?? "true";

builder.Configuration["EmailSettings:SmtpServer"] = smtpServer;
builder.Configuration["EmailSettings:SmtpPort"] = smtpPort;
builder.Configuration["EmailSettings:SenderEmail"] = senderEmail;
builder.Configuration["EmailSettings:SenderPassword"] = senderPassword;
builder.Configuration["EmailSettings:SenderName"] = senderName;
builder.Configuration["EmailSettings:EnableSSL"] = enableSSL;

// Override do CORS
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS") ?? "http://localhost:3000,http://localhost:3001";
builder.Configuration["CORS_ORIGINS"] = corsOrigins;


builder.Configuration["FIREBASE_BUCKET"] = Environment.GetEnvironmentVariable("FIREBASE_BUCKET");
builder.Configuration["GOOGLE_APPLICATION_CREDENTIALS"] = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

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

    // Mensagens personalizadas para 401 e 403
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
            return context.Response.WriteAsync("{\"message\": \"Acesso negado: seu perfil n�o tem permissão para esta a��o.\"}");
        }
    };
});

// Generic Repository / Service
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
builder.Services.AddScoped<IRepositoryCampeonato, RepositoryCampeonato>();
builder.Services.AddScoped<IRepositoryTime, RepositoryTime>();
builder.Services.AddScoped<IStorageService, ImageStorageService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Desabilitado para aceitar HTTP em desenvolvimento
// app.UseHttpsRedirection();

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

    // Criar roles
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Criar usuário admin padrão
    var adminEmail = "admin@kivo.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        // 1. Criar IdentityUser
        var newAdmin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newAdmin, "Admin@123456");

        if (result.Succeeded)
        {
            // 2. Adicionar role
            await userManager.AddToRoleAsync(newAdmin, "Administrador");

            // 3. Criar usuário na tabela Kivo
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
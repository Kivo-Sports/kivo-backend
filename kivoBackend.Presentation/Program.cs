using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Interfaces;
using kivoBackend.Infrastructure.Data;
using kivoBackend.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

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

// Generic Repository / Service
builder.Services.AddScoped(typeof(IRepositoryGenerics<>), typeof(RepositoryGenerics<>));
builder.Services.AddScoped(typeof(IServiceGenerics<>), typeof(ServiceGenerics<>));
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IRepositoryUsuario, RepositoryUsuario>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Administrador", "Torcedor", "OrganizadorTime", "OrganizadorCampeonato" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

app.Run();
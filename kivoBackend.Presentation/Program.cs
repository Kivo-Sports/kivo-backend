using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Interfaces;
using kivoBackend.Infrastructure.Data;
using kivoBackend.Infrastructure.Repositories;

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

// Generic Repository / Service
builder.Services.AddScoped(typeof(IRepositoryGenerics<>), typeof(RepositoryGenerics<>));
builder.Services.AddScoped(typeof(IServiceGenerics<>), typeof(ServiceGenerics<>));

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

app.Run();
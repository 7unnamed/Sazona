using Auth.Api.Endpoints;
using Auth.Application.Interfaces;
using Auth.Application.Services;
using Auth.Infrastructure.Data;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Security;
using BuildingBlocks.Application.Interfaces;
using BuildingBlocks.Infrastructure.Auth;
using BuildingBlocks.Infrastructure.Logging;
using BuildingBlocks.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.UseSharedSerilog("Auth.Api");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var authConnectionString = builder.Configuration.GetConnectionString("AuthDb");
var postgresPassword = builder.Configuration["POSTGRES_PASSWORD"];
if (!string.IsNullOrEmpty(postgresPassword))
{
    var csBuilder = new NpgsqlConnectionStringBuilder(authConnectionString) { Password = postgresPassword };
    authConnectionString = csBuilder.ConnectionString;
}

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(authConnectionString));

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSharedRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.Run();

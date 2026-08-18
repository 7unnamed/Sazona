using BuildingBlocks.Application.Interfaces;
using BuildingBlocks.Infrastructure.Auth;
using BuildingBlocks.Infrastructure.Logging;
using BuildingBlocks.Infrastructure.Services;
using Meals.Api.Endpoints;
using Meals.Application.Interfaces;
using Meals.Application.Services;
using Meals.Infrastructure.Data;
using Meals.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.UseSharedSerilog("Meals.Api");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var mealsConnectionString = builder.Configuration.GetConnectionString("MealsDb");
var postgresPassword = builder.Configuration["POSTGRES_PASSWORD"];
if (!string.IsNullOrEmpty(postgresPassword))
{
    var csBuilder = new NpgsqlConnectionStringBuilder(mealsConnectionString) { Password = postgresPassword };
    mealsConnectionString = csBuilder.ConnectionString;
}

builder.Services.AddDbContext<MealsDbContext>(options =>
    options.UseNpgsql(mealsConnectionString));

builder.Services.AddScoped<IPlatoRepository, PlatoRepository>();
builder.Services.AddScoped<IPlatoService, PlatoService>();

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

app.MapPlatoEndpoints();
app.MapIngredienteEndpoints();

app.Run();

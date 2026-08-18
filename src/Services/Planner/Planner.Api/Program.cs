using BuildingBlocks.Application.Interfaces;
using BuildingBlocks.Infrastructure.Auth;
using BuildingBlocks.Infrastructure.Logging;
using BuildingBlocks.Infrastructure.Services;
using Planner.Api.Endpoints;
using Planner.Application.Interfaces;
using Planner.Application.Services;
using Planner.Infrastructure.Data;
using Planner.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.UseSharedSerilog("Planner.Api");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var plannerConnectionString = builder.Configuration.GetConnectionString("PlannerDb");
var postgresPassword = builder.Configuration["POSTGRES_PASSWORD"];
if (!string.IsNullOrEmpty(postgresPassword))
{
    var csBuilder = new NpgsqlConnectionStringBuilder(plannerConnectionString) { Password = postgresPassword };
    plannerConnectionString = csBuilder.ConnectionString;
}

builder.Services.AddDbContext<PlannerDbContext>(options =>
    options.UseNpgsql(plannerConnectionString));

builder.Services.AddScoped<IHistorialEntryRepository, HistorialEntryRepository>();
builder.Services.AddScoped<IHistorialEntryService, HistorialEntryService>();

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

app.MapHistorialEntryEndpoints();

app.Run();

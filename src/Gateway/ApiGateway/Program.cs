using BuildingBlocks.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.UseSharedSerilog("ApiGateway");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Sazona (Flutter Web) llama al Gateway desde el navegador, en un origen
// distinto (localhost:puerto en dev, el dominio de hosting en producción).
// La autenticación viaja por header Authorization (no cookies), así que no
// hace falta AllowCredentials.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSharedRequestLogging();

app.UseHttpsRedirection();

app.UseCors();

app.MapReverseProxy();

app.Run();

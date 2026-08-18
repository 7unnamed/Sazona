using BuildingBlocks.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.UseSharedSerilog("ApiGateway");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSharedRequestLogging();

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();

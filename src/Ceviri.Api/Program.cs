using Ceviri.Api.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Neon Postgres
builder.Services.AddSingleton(
    NpgsqlDataSource.Create(Database.GetConnectionString(builder.Configuration, builder.Environment.ContentRootPath)));
builder.Services.AddSingleton<GlossaryRepository>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

await Database.InitializeAsync(app.Services.GetRequiredService<NpgsqlDataSource>(), app.Environment.ContentRootPath);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

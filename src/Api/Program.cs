using System.Text.Json;
using Api;
using Postgres;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<UsersDbContext>("users-db");
builder.AddElasticClientsElasticsearch("elasticsearch");
builder.AddMongoDBClient("posts-db");
builder.AddRabbitMQClient("messaging");

var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.ConfigureHttpJsonOptions(json =>
{
    json.SerializerOptions.PropertyNamingPolicy =
        JsonNamingPolicy.KebabCaseLower;
    json.SerializerOptions.WriteIndented = true;
});

builder.AddRedisOutputCache("cache");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapUsersEndpoints();
app.MapPostsEndpoints();

if (builder.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

app.MapDefaultEndpoints();
app.UseOutputCache();

app.Run();

using Api;
using Postgres;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<UsersDbContext>("users-db");
builder.AddMongoDBClient("posts-db");
builder.AddRabbitMQClient("messaging");

var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
builder.AddRedisOutputCache("cache");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapUsersEndpoints();
app.MapPostsEndpoints();
app.MapDefaultEndpoints();
app.UseOutputCache();

app.Run();

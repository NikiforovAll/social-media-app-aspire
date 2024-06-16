using Api;
using Postgres;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<UsersDbContext>("usersdb");

var app = builder.Build();

app.MapUsersEndpoints();
app.MapDefaultEndpoints();

app.Run();

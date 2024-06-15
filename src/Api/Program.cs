using Api;
using Api.Data;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

builder.AddServiceDefaults();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
builder.AddNpgsqlDbContext<SocialMediaContext>("usersdb");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapUsersEndpoints();
app.MapDefaultEndpoints();

app.Run();

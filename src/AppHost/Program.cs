var builder = DistributedApplication.CreateBuilder(args);

// Add a parameter
var pAdmin = builder.AddParameter("postgres-admin");
var admin = builder.AddParameter("admin");
var password = builder.AddParameter("admin-password", secret: true);

var redis = builder.AddRedis("cache");
var messageBus = builder
    .AddRabbitMQ("messaging", admin, password)
    .WithDataVolume()
    .WithManagementPlugin();

var usersDb = builder
    .AddPostgres("dbserver", pAdmin, password)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("users-db");

var postsDb = builder
    .AddMongoDB("posts-mongodb")
    .WithDataVolume()
    .WithMongoExpress()
    .AddDatabase("posts-db");

var elastic = builder
    .AddElasticsearch("elasticsearch", password)
    .WithDataVolume();

var api = builder
    .AddProject<Projects.Api>("api")
    .WithReference(usersDb)
    .WithReference(postsDb)
    .WithReference(elastic)
    .WithReference(redis)
    .WithReference(messageBus);

var migrator = builder
    .AddProject<Projects.MigrationService>("migrator")
    .WithReference(postsDb)
    .WithReference(elastic)
    .WithReference(usersDb);

builder.Build().Run();

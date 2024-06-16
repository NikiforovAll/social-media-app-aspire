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

// username: elastic
var postsSearchDb = builder
    .AddElasticsearch("posts-elasticsearch", password)
    .WithDataVolume();

var api = builder
    .AddProject<Projects.Api>("api")
    .WithReference(usersDb)
    .WithReference(postsDb)
    .WithReference(postsSearchDb)
    .WithReference(redis)
    .WithReference(messageBus)
    .WithReplicas(1);

var migrator = builder
    .AddProject<Projects.MigrationService>("migrator")
    .WithReference(postsDb)
    .WithReference(usersDb);

builder.Build().Run();

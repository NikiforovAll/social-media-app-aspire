var builder = DistributedApplication.CreateBuilder(args);

// Add a parameter
var admin = builder.AddParameter("admin");
var password = builder.AddParameter("admin-password", secret: true);

var redis = builder.AddRedis("cache");

var usersDb = builder
    .AddPostgres("dbserver", admin, password)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("usersdb");

var postsDb = builder
    .AddMongoDB("posts")
    .WithMongoExpress()
    .AddDatabase("postsdb");

// username: elastic
var postsSearchDb = builder
    .AddElasticsearch("posts-elasticsearch", password)
    .WithDataVolume();

var api = builder
    .AddProject<Projects.Api>("api")
    .WithReference(usersDb)
    .WithReference(postsDb)
    .WithReference(postsSearchDb)
    .WithReference(redis);

var migrator = builder
    .AddProject<Projects.MigrationService>("migrator")
    .WithReference(usersDb);

builder.Build().Run();

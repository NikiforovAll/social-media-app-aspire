var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache");

var usersDb = builder
    .AddPostgres("users")
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("usersdb");

var postsDb = builder
    .AddMongoDB("posts")
    .WithMongoExpress()
    .AddDatabase("postsDb");

var postsIndexDb = builder.AddElasticsearch("posts-index").WithDataVolume();

var api = builder
    .AddProject<Projects.Api>("api")
    .WithReference(usersDb)
    .WithReference(postsDb)
    .WithReference(postsIndexDb)
    .WithReference(redis);

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

// Backing stateful infrastructure orchestrated for local development
var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg17");

var nettutosDb = postgres.AddDatabase("nettutos-db", "nettutos_db");
var gamificationDb = postgres.AddDatabase("gamification-db", "nettutos_gamification_db");

var redis = builder.AddRedis("redis");
var rabbitmq = builder.AddRabbitMQ("rabbitmq");
var kafka = builder.AddKafka("kafka");

// 4-Layer DDD Backend Microservices
var codeRunner = builder.AddProject<Projects.NET_Tutos_CodeRunner_API>("coderunner")
    .WithHttpHealthCheck("/healthz");

var gamification = builder.AddProject<Projects.NET_Tutos_Gamification_API>("gamification")
    .WithReference(gamificationDb)
    .WithReference(rabbitmq)
    .WithHttpHealthCheck("/healthz");

var aiTutor = builder.AddProject<Projects.NET_Tutos_AiTutor_API>("aitutor")
    .WithHttpHealthCheck("/healthz");

var catalog = builder.AddProject<Projects.NET_Tutos_Catalog_API>("catalog")
    .WithReference(nettutosDb)
    .WithHttpHealthCheck("/healthz");

// Web BFF (ASP.NET Core 10 MVC, SignalR, Hybrid RAG pgvector, Arena)
var web = builder.AddProject<Projects.web>("web")
    .WithReference(nettutosDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(kafka)
    .WithReference(codeRunner)
    .WithReference(gamification)
    .WithReference(aiTutor)
    .WithReference(catalog)
    .WithEnvironment("Services__CodeRunner__Url", codeRunner.GetEndpoint("http"))
    .WithEnvironment("Services__Gamification__Url", gamification.GetEndpoint("http"))
    .WithEnvironment("Services__AiTutor__Url", aiTutor.GetEndpoint("http"))
    .WithEnvironment("Services__Catalog__Url", catalog.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

// Microsoft YARP API Gateway (Single External Ingress)
builder.AddProject<Projects.NET_Tutos_YarpGateway>("gateway")
    .WithReference(web)
    .WithReference(codeRunner)
    .WithReference(gamification)
    .WithReference(aiTutor)
    .WithReference(catalog)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/healthz");

builder.Build().Run();

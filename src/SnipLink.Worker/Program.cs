using Serilog;
using SnipLink.Infrastructure;
using SnipLink.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, config) =>
    config.ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));

// Infrastructure gives us the DbContext and IClickAggregator. The worker does not
// need the cache or click recorder, but AddInfrastructure wires them harmlessly.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AggregationWorker>();

var host = builder.Build();
host.Run();

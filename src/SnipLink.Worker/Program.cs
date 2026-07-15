using Serilog;
using SnipLink.Infrastructure;
using SnipLink.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, config) =>
    config.ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AggregationWorker>();

var host = builder.Build();
host.Run();

using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SnipLink.Application;
using SnipLink.Infrastructure;
using SnipLink.Infrastructure.Clicks;
using SnipLink.Infrastructure.Persistence;
using SnipLink.Web;
using SnipLink.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.Configure<SnipLinkOptions>(builder.Configuration.GetSection(SnipLinkOptions.SectionName));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ClickFlushService>();

builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.CreateLink, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

await DatabaseStartup.MigrateAndSeedAsync(app);

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("SnipLink:EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();

app.MapControllers();
app.MapRazorPages();
app.MapRedirect();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

app.Run();

public partial class Program;

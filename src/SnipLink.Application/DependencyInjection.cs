using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SnipLink.Application.Abstractions;
using SnipLink.Application.Links;

namespace SnipLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILinkService, LinkService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddValidatorsFromAssemblyContaining<CreateLinkRequestValidator>();
        return services;
    }
}

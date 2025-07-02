using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using AutoMapper.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// Extensions to scan for AutoMapper classes and register the configuration, mapping, and extensions with the service collection:
/// <list type="bullet">
/// <item> Finds <see cref="Profile"/> classes and initializes a new <see cref="MapperConfiguration" />,</item> 
/// <item> Scans for <see cref="ITypeConverter{TSource,TDestination}"/>, <see cref="IValueResolver{TSource,TDestination,TDestMember}"/>, <see cref="IMemberValueResolver{TSource,TDestination,TSourceMember,TDestMember}" /> and <see cref="IMappingAction{TSource,TDestination}"/> implementations and registers them as <see cref="ServiceLifetime.Transient"/>, </item>
/// <item> Registers <see cref="IConfigurationProvider"/> as <see cref="ServiceLifetime.Singleton"/>, and</item>
/// <item> Registers <see cref="IMapper"/> as a configurable <see cref="ServiceLifetime"/> (default is <see cref="ServiceLifetime.Transient"/>)</item>
/// </list>
/// After calling AddAutoMapper you can resolve an <see cref="IMapper" /> instance from a scoped service provider, or as a dependency
/// To use <see cref="AutoMapper.QueryableExtensions.Extensions.ProjectTo{TDestination}(IQueryable,IConfigurationProvider, System.Linq.Expressions.Expression{System.Func{TDestination, object}}[])" /> you can resolve the <see cref="IConfigurationProvider"/> instance directly for from an <see cref="IMapper" /> instance.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoMapper(this IServiceCollection services, Action<IMapperConfigurationExpression> configAction)
        => AddAutoMapperClasses(services, configAction);
    
    // public static IServiceCollection AddAutoMapper(this IServiceCollection services, Action<IServiceProvider, IMapperConfigurationExpression> configAction)
    //     => AddAutoMapperClasses(services, configAction);

    private static IServiceCollection AddAutoMapperClasses(IServiceCollection services, Action<IMapperConfigurationExpression> configAction)
    {
        configAction ??=  _ => { };

        var config = new MapperConfigurationExpression();

        configAction(config);
        
        ((IGlobalConfigurationExpression)config).RegisterServices(services);

        services.AddSingleton(config);
        
        // Just return if we've already added AutoMapper to avoid double-registration
        if (services.Any(sd => sd.ServiceType == typeof(IMapper)))
        {
            return services;
        }
        services.AddSingleton<IConfigurationProvider>(sp =>
        {
            // A mapper configuration is required
            var options = sp.GetRequiredService<MapperConfigurationExpression>();
            return new MapperConfiguration(options, sp.GetRequiredService<ILoggerFactory>());
        });
        services.Add(new(typeof(IMapper), sp => new Mapper(sp.GetRequiredService<IConfigurationProvider>(), sp.GetService), config.ServiceLifetime));

        return services;
    }
}
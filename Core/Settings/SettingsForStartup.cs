using Core.Settings.Models;
using Core.Settings.OptionsStartup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Core.Settings;

public static class SettingsForStartup
{
    public static IServiceCollection AddCoreSettings(this IServiceCollection collection)
    { 
        collection.AddSettings<DbSettings>();
        collection.AddSettings<DebugSettings>();
        
        return collection;
    }
}
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Settings;

public static class ValidateSettingsExtensions
{
    public static void ValidateSettings(this IHost host)
    {
        var sp = host.Services;
        var validators = sp.GetRequiredService<IEnumerable<IValidateOptions>>();
        foreach (var validator in validators)
        {
            validator.Validate();
        }
    }
}
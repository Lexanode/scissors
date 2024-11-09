using System;
using Core.Settings;
using Microsoft.Extensions.Configuration;

namespace DiscordIS;

public class CredentialSettings : IValidateOptions
{
    public string CookieValue { get; init; }
    public string DiscordToken { get; init; }

    public CredentialSettings(IConfiguration configuration)
    {
        CookieValue = configuration.GetValue<string>(nameof(CookieValue));
        DiscordToken = configuration.GetValue<string>(nameof(DiscordToken));
    }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CookieValue))
        {
            throw new Exception("Expected a valid cookie value");
        }
        if (string.IsNullOrWhiteSpace(DiscordToken))
        {
            throw new Exception("Expected a valid discord token");
        }
    }
}
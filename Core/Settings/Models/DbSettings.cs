using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Core.Settings.Models;

/// <summary>
/// Настройки из базы
/// </summary>
public class DbSettings : IValidateOptions
{
    private const string SectionName = "DatabaseSettings";
    private const string ConnectionStringName = "ConnectionString";
    
    /// 
    public DbSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        ConnectionString = section.GetSection(ConnectionStringName).Value;
    }
    
    /// <summary>
    /// Строка подключения к бд
    /// </summary>
    public string ConnectionString { get; }

    public void Validate()
    {
        var failureMessages = new List<string>();

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            failureMessages.Add("Строка подключения пуста");
        }

        if (failureMessages.Any())
        {
            throw new OptionsValidationException(nameof(DbSettings), typeof(DbSettings), failureMessages);
        }
    }
}
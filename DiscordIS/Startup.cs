using System;
using System.Reflection;
using Core;
using Core.Settings.OptionsStartup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordIS;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection collection)
    {
        collection.AddCore(_configuration);
        collection.AddControllersWithViews().AddApplicationPart(Assembly.GetCallingAssembly());
        collection.AddSettings<CredentialSettings>();
        collection.AddHostedService<MainHostedService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseCore(env);
        app.UseEndpoints(x => x.MapControllers());
    }
}
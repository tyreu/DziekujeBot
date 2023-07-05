using DziekujeBot;
using DziekujeBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.OutputEncoding = System.Text.Encoding.UTF8;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IDziekujeService, DziekujeService>();
    })
    .Build();
_ = new Dziekuje(host.Services.GetService<IDziekujeService>());
Console.ReadKey();
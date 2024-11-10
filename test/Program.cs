using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace test;

public static class Program
{
    public static Task Main(string[] args)
    {
        var timeout = GetTimeout();
        var channel = Channel.CreateUnbounded<string>();
        
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<InputDataService>( _ => new InputDataService(channel));
                services.AddSingleton<DomainCheckService>( _ => new DomainCheckService(channel, timeout));
                services.AddHostedService<WorkerHostedService>();
            })
            .Build();
        
        var cts = new CancellationTokenSource();
        
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        
        return host.RunAsync(cts.Token);
    }

    private static int GetTimeout()
    {
        Console.WriteLine("Введите первоначальную настройку частоты опроса в секундах");
        
        while (true)
        {
            var text = Console.ReadLine();
            
            if (int.TryParse(text, out var number))
            {
                return number;
            }

            Console.WriteLine("Вы ввели не число");
        }
    }
}
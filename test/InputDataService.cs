using System.Threading.Channels;

namespace test;

public sealed class InputDataService(Channel<string> channel)
{
    private readonly ChannelWriter<string> _channelWriter = channel.Writer;
    private const string HttpTemplate = "http";

    public async Task WorkAsync(CancellationToken cancellationToken)
    {
        await ExecuteStartDomainDataAsync(cancellationToken); // начальные данные
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var text = Console.ReadLine();
            await TryAddNewDomainAsync(cancellationToken, text);
            await Task.Delay(10, cancellationToken);
        }
    }
    
    private async Task ExecuteStartDomainDataAsync(CancellationToken cancellationToken)
    {
        var startDomainForAdd = new[]
        {
            "http://ru.drivemusic.me/",
            "https://rus.hitmotop.com/",
            "https://pinkamuz.pro/",
            "https://mp3bob.ru/",
            "http://vuxo7.com/"
        };

        Console.WriteLine("Добавляю первоначальные домены для проверки");

        foreach (var domain in startDomainForAdd)
        {
            await TryAddNewDomainAsync(cancellationToken, domain);
        }
    }

    private async Task TryAddNewDomainAsync(CancellationToken cancellationToken, string? text)
    {
        if (text is not null && text.Contains(HttpTemplate, StringComparison.CurrentCultureIgnoreCase))
        {
            await _channelWriter.WriteAsync(text, cancellationToken);
        }
    }
}
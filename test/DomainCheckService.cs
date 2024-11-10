using System.Collections.Concurrent;
using System.Threading.Channels;
using Timer = System.Timers.Timer;

namespace test;

public sealed class DomainCheckService(Channel<string> channel, int timeoutValueInSeconds)
{
    private readonly HttpClient _httpClient = new();
    private readonly ConcurrentDictionary<string, DomainStateModel> _domainStateModels = new();
    private readonly ChannelReader<string> _channelReader = channel.Reader;
    private readonly Timer _timer = new(timeoutValueInSeconds * SecToMillisecondCoefficient);
    
    private const string AdvScriptsUrl = "https://scripts.advmusic.com/";
    private const int SecToMillisecondCoefficient = 1000;

    public Task CreateTimerForDomainUpdateAsync(CancellationToken cancellationToken)
    {
        _timer.Elapsed += async ( _, _ ) => await CreateCheckTask(cancellationToken);
        _timer.Start();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            Task.Delay(10, cancellationToken);
        }

        _timer.Stop();
        _timer.Dispose();
        
        return Task.CompletedTask;
    }
    
    public async Task UpdateDomainListAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _channelReader.ReadAllAsync(cancellationToken))
        {
            if (_domainStateModels.TryGetValue(message, out _))
            {
                Console.WriteLine($"Введенный домен: {message} уже существует");
                continue;
            }

            _domainStateModels.TryAdd(message, new DomainStateModel()
            {
                LastState = false
            });
            
            Console.WriteLine($"Введенный домен: {message} успешно добавлен в обработку" ); 
        }
    }
    
    private async Task CreateCheckTask(CancellationToken cancellationToken)
    {
        var domainsForCheck = _domainStateModels.Select(x => x.Key);
        
        var tasks = domainsForCheck.Select(x => CreateDomainStatusStringForPrint(x, cancellationToken)).ToArray();
 
        await Task.WhenAll(tasks);

        foreach (var task in tasks)
        {
            Console.WriteLine(task.Result);
        }
    }
    
    private async Task<string> CreateDomainStatusStringForPrint(string url, CancellationToken cancellationToken)
    {
        try
        {
            var currentStatus = await IsDomainContainsAdvScript(url, cancellationToken);

            var domainStateModel = TryGetDomainStateModelByUrl(url);

            var result = GetResultDomainStatusString(url, currentStatus, domainStateModel);

            TryUpdateDomainStateModelByUrl(url, currentStatus);

            return result;
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            var error = $"{ex.StackTrace} : {ex.Message} : {ex.InnerException}";
            return $"При проверке {url} произошла ошибка : {error}";
        }
    }

    private static string GetResultDomainStatusString(
        string url, 
        bool currentStatus, 
        DomainStateModel domainStateModel)
    {
        return currentStatus switch
        {
            true when domainStateModel.LastState is false &&
                      domainStateModel.LastSuccessfulCheckDateTime is not null
                => string.Format("{0} : ok // ресурс был недоступен {1} секунд", 
                    url, 
                    DateTimeOffset.UtcNow.Subtract((DateTimeOffset)domainStateModel.LastSuccessfulCheckDateTime).
                        TotalSeconds),
            true => $"{url} : ok",
            false => $"{url} : fail"
        };
    }

    private DomainStateModel TryGetDomainStateModelByUrl(string url)
    {
        _domainStateModels.TryGetValue(url, out var item);
        
        if (item is null)
        {
            throw new InvalidOperationException("DomainStateModel не может быть пустым");
        }
        
        return item;
    }

    private void TryUpdateDomainStateModelByUrl(string key, bool isActive)
    {
        DateTimeOffset? dateTime = isActive ? DateTimeOffset.UtcNow : null;
        
        _domainStateModels.AddOrUpdate(
            key,
            _ => new DomainStateModel { LastState = isActive, LastSuccessfulCheckDateTime = dateTime },
            (_, existingValue) =>
            {
                existingValue.LastSuccessfulCheckDateTime = isActive ? 
                    DateTimeOffset.UtcNow : existingValue.LastSuccessfulCheckDateTime;;
                existingValue.LastState = isActive;
                return existingValue; 
            });
    }

    private async Task<bool> IsDomainContainsAdvScript(string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return content.Contains(AdvScriptsUrl);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
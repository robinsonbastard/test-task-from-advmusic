using Microsoft.Extensions.Hosting;

namespace test;

public sealed class WorkerHostedService(InputDataService inputDataService, DomainCheckService domainCheckService) 
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => domainCheckService.CreateTimerForDomainUpdateAsync(cancellationToken), cancellationToken);
        Task.Run(() => domainCheckService.UpdateDomainListAsync(cancellationToken), cancellationToken);
        Task.Run(() => inputDataService.WorkAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }
}
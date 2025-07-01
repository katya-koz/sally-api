using SALLY_API.Entities;
using SALLY_API;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;
using SALLY_API.WebServices;

public class InMemoryQueueService
{
    public ConcurrentQueue<ADUser> UserQueue { get; } = new();

    public ConcurrentDictionary<string, ADUser> inProcessUsers = new();
    public SemaphoreSlim QueueNotifier { get; } = new(0);
    public ConcurrentDictionary<string, TaskCompletionSource<UpsertResult>> TaskCompletionSources { get; } = new();

}

public class UserQueueWorker : BackgroundService
{
    private readonly InMemoryQueueService _queueService;
    private readonly IServiceProvider _serviceProvider;

    public UserQueueWorker(InMemoryQueueService queueService, IServiceProvider serviceProvider)
    {
        _queueService = queueService;
        _serviceProvider = serviceProvider;
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _queueService.QueueNotifier.WaitAsync(stoppingToken);

            if (_queueService.UserQueue.TryDequeue(out var user))
            {
                using var scope = _serviceProvider.CreateScope();
                var apiService = scope.ServiceProvider.GetRequiredService<APIService>();

                try
                {
                  
                    var result = await apiService.UpsertUser(user);

                 

                    if (_queueService.TaskCompletionSources.TryGetValue(user.Username, out var completionSource))
                    {
                        completionSource.SetResult(result);
                    }
                }
                catch (Exception ex)
                {
                    if (_queueService.TaskCompletionSources.TryGetValue(user.Username, out var completionSource))
                    {
                        completionSource.SetException(ex);
                    }
                }
                finally
                {
                    _queueService.inProcessUsers.TryRemove(user.Username, out _);
                }
            }
        }
    }
}



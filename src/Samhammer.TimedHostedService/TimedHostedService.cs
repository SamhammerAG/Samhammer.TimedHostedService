using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Samhammer.TimedHostedService
{
    public abstract class TimedHostedService<T> : IHostedService, IDisposable
    {
        protected virtual TimeSpan StartDelay => TimeSpan.MinValue;

        protected virtual TimeSpan ExecutionInterval => TimeSpan.FromSeconds(60);

        protected virtual bool OnlySingleInstance => false;

        protected ILogger<TimedHostedService<T>> Logger { get; }

        protected IServiceScopeFactory Services { get; }

        private bool IsRunning { get; set; }

        private Timer Timer { get; set; }

        protected TimedHostedService(ILogger<TimedHostedService<T>> logger, IServiceScopeFactory services)
        {
            Logger = logger;
            Services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await OnStartup();

            Logger.LogInformation("TimedHostedService {HostedService} is starting. (Tick interval: {Interval})", typeof(T), ExecutionInterval);

            Timer = new Timer(TimerTickCallback, null, StartDelay, ExecutionInterval);
        }

        protected virtual Task OnStartup()
        {
            return Task.CompletedTask;
        }

        private async void TimerTickCallback(object state)
        {
            try
            {
                Logger.LogDebug("TimedHostedService {HostedService} tick starting.", typeof(T));
                if (OnlySingleInstance && IsRunning)
                {
                    Logger.LogDebug("TimedHostedService {HostedService} tick stopping, because one instance is already running.", typeof(T));
                    return;
                }

                IsRunning = true;
                await Run();
                await OnRunSuccessful();
                IsRunning = false;

                Logger.LogDebug("TimedHostedService {HostedService} tick finished.", typeof(T));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "TimedHostedService {HostedService} tick failed with an error, aborting current tick.", typeof(T));
                await OnError();
                IsRunning = false;
            }
        }

        protected virtual Task OnRunSuccessful()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnError()
        {
            return Task.CompletedTask;
        }

        private async Task Run()
        {
            using (var scope = Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<T>();
                await RunScoped(service);
            }
        }

        protected abstract Task RunScoped(T service);

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await OnStopping();

            Logger.LogInformation("TimedHostedService {HostedService} is stopping.", typeof(T));

            Timer?.Change(Timeout.Infinite, 0);
        }

        protected virtual Task OnStopping()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Samhammer.TimedHostedService.Test
{
    public class TimedHostedServiceTest
    {
        private ISampleService SampleService { get; }

        private IServiceCollection Services { get; }

        private ServiceProvider ServiceProvider => Services.BuildServiceProvider();

        public TimedHostedServiceTest()
        {
            var logger = Substitute.For<ILogger<TimedHostedService<ISampleService>>>();
            SampleService = Substitute.For<ISampleService>();

            Services = new ServiceCollection();
            Services.AddSingleton(logger);
            Services.AddHostedService<SampleHostedService>();
            Services.AddSingleton(SampleService);
        }

        [Fact]
        public async Task HostedServiceExecutes()
        {
            var cancelSource = new CancellationTokenSource();
            SampleService.When(s => s.DoWork()).Do(info => cancelSource.Cancel());

            var hostedService = ServiceProvider.GetService<IHostedService>();

            await hostedService.StartAsync(CancellationToken.None);
            await Delay(1000, cancelSource);
            await hostedService.StopAsync(CancellationToken.None);

            await SampleService.Received().DoWork();
        }

        private async Task Delay(int milliseconds, CancellationTokenSource cancelSource)
        {
            try
            {
                await Task.Delay(milliseconds, cancelSource.Token);
            }
            catch (TaskCanceledException)
            {
            }
        }

        public interface ISampleService
        {
            Task DoWork();
        }

        public class SampleHostedService : TimedHostedService<ISampleService>
        {
            public SampleHostedService(ILogger<TimedHostedService<ISampleService>> logger, IServiceScopeFactory services)
                : base(logger, services)
            {
            }

            protected override async Task RunScoped(ISampleService service)
            {
                await service.DoWork();
            }
        }
    }
}

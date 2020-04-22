[![Build Status](https://travis-ci.com/SamhammerAG/Samhammer.TimedHostedService.svg?branch=master)](https://travis-ci.com/SamhammerAG/Samhammer.TimedHostedService)

## Samhammer.TimedHostedService

This package provides a hosted service that can run periodically.

The concept is from this documentation:
https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-3.1&tabs=visual-studio

#### How to add this to your project:
- reference this nuget package: https://www.nuget.org/packages/Samhammer.TimedHostedService/

#### How to use:

Implement a class that inherits TimedHostedService.
```csharp
public class MyHostedService : TimedHostedService<IMyService>
{
	protected override TimeSpan StartDelay => TimeSpan.FromSeconds(3);

	protected override TimeSpan ExecutionInterval => TimeSpan.FromDays(1);
	
	public MyHostedService(ILogger<MyHostedService> logger, IServiceScopeFactory services) 
		: base(logger, services)
	{
	}
	
	protected override Task RunScoped(IMyService myService)
	{
		myService.DoSomething();
		return Task.CompletedTask;
	}
}
```

Register the hosted service in startup:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHostedService<MyHostedService>()
}
```

Possible configurations:
- ExecutionInterval: How often the job ticks after starting (default: 60 seconds)
- StartDelay: Delay the first tick for a specific time after starting (default: 0)
- OnlySingleInstance: If the last tick is still no other tick is started even if the executionInterval is over (default: false)

Possible hook points:
- RunScoped: Execute your logic here (mandatory)
- OnStartup: Is called before the timer starts ticking (optional)
- OnRunSuccessful: Is executed after the current tick is over (optional)
- OnError:  Can be used to handle errors that occur within a running tick

Note:
- Errors inside "ExecutionInterval" are catched and can be handled with "OnError"
- "RunScoped" is running in it's ioc scope. The other hook points are outside of this scope.

## Contribute

#### How to publish a nuget package
- set package version in Samhammer.TimedHostedService.csproj
- create git tag
- dotnet pack -c Release
- nuget push .Samhammer.TimedHostedService\bin\Release\Samhammer.TimedHostedService.*.nupkg NUGET_API_KEY -src https://api.nuget.org/v3/index.json
- (optional) nuget setapikey NUGET_API_KEY -source https://api.nuget.org/v3/index.json

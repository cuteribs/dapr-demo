using Dapr.Client;
using Dapr.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;

namespace web;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		var services = builder.Services;
		var configuration = builder.Configuration;

		var daprClient = new DaprClientBuilder().Build();

		// merge dapr configuration store into configuration
		configuration.AddDaprConfigurationStore(configuration["Dapr:ConfigStore"]!, new[] { "name" }, daprClient, TimeSpan.FromSeconds(30));

		// merge dapr secret store into configuration
		configuration.AddDaprSecretStore(configuration["Dapr:SecretStore"]!, daprClient);

		services.AddControllers()
			// register DaprClient
			.AddDapr();

		services.AddDataProtection()
			.SetApplicationName("web")
			.ProtectKeysWithDaprCrypto("cryptostore", "protectionkey");

		var app = builder.Build();

		app.UseDeveloperExceptionPage();

		app.UseCloudEvents();
		app.MapSubscribeHandler();

		app.MapControllers();

		app.Run();
	}
}

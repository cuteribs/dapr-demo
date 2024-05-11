using Dapr.Client;
using Dapr.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Identity.Web;

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

		services.AddControllersWithViews()
			// register DaprClient
			.AddDapr();
		//services.AddMvc();
		//services.AddControllers()

		services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
			.AddMicrosoftIdentityWebApp(configuration.GetSection("Oidc"))
			.EnableTokenAcquisitionToCallDownstreamApi()
			.AddDistributedTokenCaches();

		services.AddDistributedMemoryCache();

		services.AddDataProtection()
			.SetApplicationName("web")
			.PersistKeysToDaprStateStore("statestore")
			.ProtectKeysWithDaprCrypto("cryptostore", "protectionkey");

		var app = builder.Build();


		app.UseDeveloperExceptionPage();
		app.UseRouting();
		app.UseAuthentication().UseAuthorization();
		app.MapDefaultControllerRoute();

		app.UseCloudEvents();
		app.MapSubscribeHandler();

		app.Run();
	}
}

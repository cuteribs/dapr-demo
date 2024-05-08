using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace web;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		var services = builder.Services;
		var configuration = builder.Configuration;

		services.AddControllers()
			.AddDapr();

		services.AddDataProtection()
			.SetApplicationName("web")
			.ProtectKeysWithDaprCrypto("cryptostore", "protectionkey");

		var app = builder.Build();

		app.UseDeveloperExceptionPage();

		//app.UseCloudEvents();
		//app.MapSubscribeHandler();

		app.MapControllers();

		app.Run();
	}
}

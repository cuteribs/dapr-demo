using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

[ApiController]
[Produces("application/json")]
[Route("[controller]")]
public class TestController : ControllerBase
{
	private readonly ILogger<TestController> _logger;

	public TestController(ILogger<TestController> logger)
	{
		_logger = logger;
	}

	[HttpGet("config")]
	public IEnumerable<KeyValuePair<string, string?>> Configuration(string? key = null)
	{
		_logger.LogWarning("Retrieving configuration");
		var configuration = this.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
		var items = configuration.AsEnumerable().OrderBy(x => x.Key);
		return key == null ? items : items.Where(x => x.Key == key);
	}

	[HttpGet("secret")]
	public async Task<string> Secret()
	{
		_logger.LogWarning("Retrieving secret: 'ConnectionStrings:Db'");
		var client = this.HttpContext.RequestServices.GetRequiredService<DaprClient>();
		var result = await client.GetSecretAsync("secretstore", "ConnectionStrings:Db");
		return result.First().Value;
	}

	[HttpGet("state")]
	public async Task<string> State(string key, string value)
	{
		_logger.LogWarning("Saving state: '{key}': '{value}'", key, value);
		var client = this.HttpContext.RequestServices.GetRequiredService<DaprClient>();
		await client.SaveStateAsync("statestore", key, $"{value}{Guid.NewGuid()}");

		_logger.LogWarning("Retrieving state: '{key}'", key);
		value = await client.GetStateAsync<string>("statestore", key);
		return value;
	}

	[HttpGet("crypto")]
	public IEnumerable<string> Crypto(string text)
	{
		var protector = this.HttpContext.RequestServices.GetRequiredService<IDataProtectionProvider>()
			.CreateProtector("test");
		var list = new List<string>();
		list.Add(text);

		_logger.LogWarning("Encrypting: {text}", text);
		var protectedStr = protector.Protect(text);
		list.Add(protectedStr);

		_logger.LogWarning("Decrypting: {text}", protectedStr);
		var unprotectedStr = protector.Unprotect(protectedStr);
		list.Add(unprotectedStr);

		return list;
	}

	[HttpGet("pub")]
	public async Task Pub(string text)
	{
		_logger.LogWarning("Sending message: {message}", text);
		var client = this.HttpContext.RequestServices.GetRequiredService<DaprClient>();
		var message = new MessageEvent(Guid.NewGuid().ToString(), text);
		await client.PublishEventAsync("eventbroker", "A", message);
	}

	[HttpPost("sub")]
	[Topic("eventbroker", "A")]
	public void Sub(MessageEvent message)
	{
		_logger.LogWarning("Message received: {message}", message.Message);
	}
}

public record MessageEvent(string MessageType, string Message);

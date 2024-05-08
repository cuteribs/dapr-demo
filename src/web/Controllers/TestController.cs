using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class TestController : ControllerBase
	{
		private readonly ILogger<TestController> _logger;
		private readonly IDataProtector _protector;
		private readonly DaprClient _daprClient;

		public TestController(
			ILogger<TestController> logger,
			IDataProtectionProvider dataProtectionProvider, 
			DaprClient daprClient
		)
		{
			_logger = logger;
			_protector = dataProtectionProvider.CreateProtector("test");
			_daprClient = daprClient;
		}

		[HttpGet("test")]
		public IEnumerable<string> Test(string str)
		{
			var list = new List<string>();
			list.Add(str);

			var protectedStr = _protector.Protect(str);
			list.Add(protectedStr);

			var unprotectedStr = _protector.Unprotect(protectedStr);
			list.Add(unprotectedStr);

			return list;
		}

		[HttpPost("pub")]
		public async Task Pub([FromBody] string text)
		{
			var message = new MessageEvent(Guid.NewGuid().ToString(), text);
			await _daprClient.PublishEventAsync("eventbroker", "A", message);
			_logger.LogWarning("Message sent: {message}", message.Message);
		}

		[HttpPost("sub")]
		[Topic("eventbroker", "A")]
		public void Sub(MessageEvent message)
		{
			_logger.LogWarning("Message received: {message}", message.Message);
		}
	}

	public record MessageEvent(string MessageType, string Message);
}

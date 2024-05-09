using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

[Route("[controller]")]
public class DefaultController : ControllerBase
{
	[HttpGet]
	public string Get()
	{
		return "Hello from web!";
	}
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace web.Controllers;

[Authorize]
public class HomeController : Controller
{
	[AllowAnonymous]
	public IActionResult Index()
	{
		return View();
	}

	public async  Task<IActionResult> Signin()
	{
		var configuration = this.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
		var scopes = configuration["DownstreamApi:Scopes"]!;
		var tokenAcquisition = this.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
		this.ViewBag.Token = await tokenAcquisition.GetAccessTokenForUserAsync([scopes]);
		return View();
	}

	[AllowAnonymous]
	public async Task<IActionResult> Signout()
	{
		await this.HttpContext.SignOutAsync();
		return this.RedirectToAction("Index");
	}
}

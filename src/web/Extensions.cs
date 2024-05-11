using Dapr.Client;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace web;

public static class Extensions
{
	public static IDataProtectionBuilder PersistKeysToDaprStateStore(
		this IDataProtectionBuilder builder,
		string componentName,
		string? keyName = "DataProtection-Keys"
	) => builder.PersistKeysToDaprStateStore(o =>
	{
		o.ComponentName = componentName;
		o.KeyName = keyName;
	});

	public static IDataProtectionBuilder PersistKeysToDaprStateStore(
		this IDataProtectionBuilder builder,
		Action<DaprStateStoreOptions> configureOptions
	)
	{
		builder.Services
			.Configure(configureOptions)
			.Configure<KeyManagementOptions>((sp, o) => o.XmlRepository = new DaprStateStoreXmlRepository(sp));
		return builder;
	}

	public static IDataProtectionBuilder ProtectKeysWithDaprCrypto(
		this IDataProtectionBuilder builder,
		string componentName,
		string keyName
	) => builder.ProtectKeysWithDaprCrypto(o =>
	{
		o.ComponentName = componentName;
		o.KeyName = keyName;
	});

	public static IDataProtectionBuilder ProtectKeysWithDaprCrypto(
		this IDataProtectionBuilder builder,
		Action<DaprXmlCipherOptions> configureOptions
	)
	{
		builder.Services.AddSingleton<DaprXmlCipher>()
			.Configure<DaprXmlCipherOptions>(configureOptions)
			.Configure<KeyManagementOptions>((sp, o) => o.XmlEncryptor = sp.GetRequiredService<DaprXmlCipher>());
		return builder;
	}

	public static IServiceCollection Configure<TOptions>(
		this IServiceCollection services,
		Action<IServiceProvider, TOptions> configureOptions
	) where TOptions : class
		=> services.AddOptions()
			.AddSingleton<IConfigureOptions<TOptions>>(p => new ConfigureOptions<TOptions>(o => configureOptions(p, o)));

	public static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder EnableTokenAcquisitionToCallDownstreamApi(
		this MicrosoftIdentityWebAppAuthenticationBuilderWithConfiguration builder,
		string initialScopes
	) => builder.EnableTokenAcquisitionToCallDownstreamApi(initialScopes.Split(' '));

	public static IServiceCollection AddDistributedDaprCache(this IServiceCollection services, Action<DaprCacheOptions> setupAction)
	{
		services.AddOptions();
		services.AddSingleton<IDistributedCache, DaprCache>();
		services.Configure(setupAction);
		return services;
	}
}

public class DaprCacheOptions
{
	public string? ComponentName { get; set; }
	public string? KeyName { get; set; }
}

public class DaprCache : IDistributedCache
{
	private readonly ILogger _logger;
	private readonly DaprCacheOptions _options;

	public DaprCache(IOptions<DaprCacheOptions> options)
	{
		_options = options.Value;
	}

	public byte[]? Get(string key)
	{
		throw new NotImplementedException();
	}

	public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
	{
		throw new NotImplementedException();
	}

	public void Refresh(string key)
	{
		throw new NotImplementedException();
	}

	public Task RefreshAsync(string key, CancellationToken token = default)
	{
		throw new NotImplementedException();
	}

	public void Remove(string key)
	{
		throw new NotImplementedException();
	}

	public Task RemoveAsync(string key, CancellationToken token = default)
	{
		throw new NotImplementedException();
	}

	public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
	{
		throw new NotImplementedException();
	}

	public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
	{
		throw new NotImplementedException();
	}
}

public class DaprStateStoreOptions
{
	public string? ComponentName { get; set; }
	public string? KeyName { get; set; }
}

public class DaprXmlCipherOptions
{
	public string? ComponentName { get; set; }
	public string? KeyName { get; set; }
}

public class DaprStateStoreXmlRepository : IXmlRepository
{
	private static readonly string KeysName = "keys";

	private readonly DaprClient _daprClient;
	private readonly DaprStateStoreOptions _options;

	public DaprStateStoreXmlRepository(IServiceProvider serviceProvider)
		: this(
			serviceProvider.GetRequiredService<DaprClient>(),
			serviceProvider.GetRequiredService<IOptions<DaprStateStoreOptions>>().Value
		)
	{
	}

	public DaprStateStoreXmlRepository(DaprClient daprClient, DaprStateStoreOptions options)
	{
		_daprClient = daprClient;
		_options = options;
	}

	public virtual IReadOnlyCollection<XElement> GetAllElements()
		=> this.GetAllElementsCore()
			.GetAwaiter()
			.GetResult()
			.ToArray();

	public virtual void StoreElement(XElement element, string friendlyName)
	{
		var keys = this.GetAllElementsCore()
			.GetAwaiter()
			.GetResult()
			.ToList();
		keys.Add(element);
		var xml = new XElement(KeysName, keys).ToString();
		_daprClient.SaveStateAsync(_options.ComponentName, _options.KeyName, xml)
			.GetAwaiter()
			.GetResult();
	}

	private async Task<IEnumerable<XElement>> GetAllElementsCore()
	{
		var xml = await _daprClient.GetStateAsync<string>(_options.ComponentName, _options.KeyName);
		var keys = xml == null ? null : XElement.Parse(xml).Element(KeysName);
		return keys?.Elements() ?? Enumerable.Empty<XElement>();
	}
}

public class DaprXmlCipher : IXmlEncryptor, IXmlDecryptor
{
	private readonly DaprClient _daprClient;
	private readonly DaprXmlCipherOptions _options;

	public DaprXmlCipher(IServiceProvider serviceProvider)
	{
		_daprClient = serviceProvider.GetRequiredService<DaprClient>();
		_options = serviceProvider.GetRequiredService<IOptions<DaprXmlCipherOptions>>().Value;
	}

	public virtual EncryptedXmlInfo Encrypt(XElement plaintextElement)
	{
		var key = plaintextElement.ToString(SaveOptions.DisableFormatting);
#pragma warning disable CS0618 // Type or member is obsolete
		var wrappedKey = _daprClient.EncryptAsync(
			_options.ComponentName,
			Encoding.UTF8.GetBytes(key),
			_options.KeyName,
			new EncryptionOptions(KeyWrapAlgorithm.Rsa)
		).GetAwaiter().GetResult();
#pragma warning restore CS0618 // Type or member is obsolete
		var element = new XElement(
			"encryptedKey",
			new XComment("This key is encrypted with Dapr Cryptography"),
			new XElement("value", Convert.ToBase64String(wrappedKey.Span))
		);
		return new EncryptedXmlInfo(element, typeof(DaprXmlCipher));
	}

	public virtual XElement Decrypt(XElement encryptedElement)
	{
		var wrappedKey = encryptedElement.Element("value")!.Value;
#pragma warning disable CS0618 // Type or member is obsolete
		var key = _daprClient.DecryptAsync(
			_options.ComponentName,
			Convert.FromBase64String(wrappedKey),
			_options.KeyName
		).GetAwaiter().GetResult().ToArray();
#pragma warning restore CS0618 // Type or member is obsolete
		return XElement.Parse(Encoding.UTF8.GetString(key));
	}
}

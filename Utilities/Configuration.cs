using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities;

public class Configuration
{
	readonly IConfiguration config;

	public string HostName { get; set; } = System.Net.Dns.GetHostName().ToLower();

	public Configuration(Type userSecretsType)
	{
		var builder = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("AppSettings.json", optional: true, reloadOnChange: true)
			.AddUserSecrets(userSecretsType.Assembly)
			.AddEnvironmentVariables();
		config = builder.Build();
	}

	public string GetField(string fieldName)
	{
		return config.GetValue<string>(fieldName) ?? fieldName;
	}
}

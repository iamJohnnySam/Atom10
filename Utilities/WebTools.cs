using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities;

public static class WebTools
{
	private static readonly HttpClient _httpClient = new HttpClient();

	public static bool CheckUrlExists(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
			return false;

		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(15000));
		var headRequest = new HttpRequestMessage(HttpMethod.Head, url);

		try
		{
			var response = _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token)
				.GetAwaiter().GetResult();

			if (response.StatusCode == HttpStatusCode.OK)
				return true;

			// Some servers do not support HEAD and return 405 Method Not Allowed.
			// Fall back to a GET request reading only headers.
			if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
			{
				var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
				var getResponse = _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cts.Token)
					.GetAwaiter().GetResult();
				return getResponse.StatusCode == HttpStatusCode.OK;
			}

			return false;
		}
		catch (TaskCanceledException) // timeout
		{
			return false;
		}
		catch (HttpRequestException) // network / protocol errors
		{
			return false;
		}
	}
}

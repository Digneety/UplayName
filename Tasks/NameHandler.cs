using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using UbisoftName.Models;

namespace UbisoftName.Tasks;

internal static class NameHandler
{
    private static UbisoftToken? _token;

    internal static async Task CheckForNames()
    {
        if (_token == null || _token.IsExpired) _token = await GetToken();

        var namesToCheck = await File.ReadAllLinesAsync("names.txt");
        const string resultingFilePath = "availableNames.txt";

        foreach (var name in namesToCheck)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri =
                    new Uri($"https://public-ubiservices.ubi.com/v3/profiles?nameOnPlatform={name}&platformType=uplay"),
                Headers = { Authorization = new AuthenticationHeaderValue("Ubi_v1", $"t={_token?.Ticket}") }
            };

            var response = await GetHttpClient().SendAsync(request);
            var content = JsonConvert.DeserializeObject<UbisoftProfile?>(await response.Content.ReadAsStringAsync());


            if ((bool)content?.Profiles.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{name}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{name}");
                Console.ResetColor();
                await File.AppendAllLinesAsync(resultingFilePath, new[] { name + "(Available or Restricted)" });
            }
        }
    }

    private static async Task<UbisoftToken?> GetToken()
    {
        var basicCredentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["mail"] + ":" +
                                   ConfigurationManager.AppSettings["password"]));

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", basicCredentials) },
            RequestUri = new Uri("https://public-ubiservices.ubi.com/v3/profiles/sessions"),
            Content = new StringContent("{\"Content-Type\": \"application/json\"}", Encoding.UTF8,
                "application/json")
        };

        var response = await GetHttpClient().SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new UnauthorizedAccessException("Unable to retrieve authentication ticket.");
        return JsonConvert.DeserializeObject<UbisoftToken?>(await response.Content.ReadAsStringAsync())!;
    }

    private static HttpClient GetHttpClient()
    {
        var httpClientHandler = new HttpClientHandler
        {
            Proxy = new WebProxy(
                ConfigurationManager.AppSettings["proxy"] == string.Empty
                    ? null
                    : ConfigurationManager.AppSettings["proxy"], false),
            UseProxy = true
        };

        var httpClient = new HttpClient(httpClientHandler);
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Ubi-AppId", "afb4b43c-f1f7-41b7-bcef-a635d8c83822");
        httpClient.DefaultRequestHeaders.Add("Ubi-RequestedPlatformType", "uplay");
        return httpClient;
    }
}
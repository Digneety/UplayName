using System.Net;
using System.Net.Http.Headers;
using System.Text;
using NameChecker.Models;
using Newtonsoft.Json;

namespace NameChecker.Tasks;

public class NameTask
{
    private static Token? _token;
    private static uint _namesChecked;
    private static uint _namesAvailable;

    public static async Task CheckNamesAsync()
    {
        if (_token == null || _token.IsExpired)
        {
            _token = await GetToken();
        }

        var client = await GetProxyHttpClient();

        var namesToCheck = await File.ReadAllLinesAsync("names.txt");
        const string resultingFilePath = "availableNames.txt";
        foreach (var name in namesToCheck)
        {
            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri =
                        new Uri(
                            $"https://public-ubiservices.ubi.com/v3/profiles?nameOnPlatform={name}&platformType=uplay"),
                    Headers =
                    {
                        Authorization = new AuthenticationHeaderValue("Ubi_v1", $"t={_token?.Ticket}")
                    },
                };
                var response = await client.SendAsync(request);
                var content =
                    JsonConvert.DeserializeObject<UbisoftProfile?>(
                        await response.Content.ReadAsStringAsync());

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    client = await GetProxyHttpClient();
                    _token = await GetToken();
                    continue;
                }

                if ((bool)content?.Profiles.Any())
                {
                    _namesChecked += 1;
                    Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable}";
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"{name}");
                    Console.ResetColor();
                }
                else
                {
                    _namesChecked += 1;
                    _namesAvailable += 1;
                    Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable}";
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{name}");
                    Console.ResetColor();
                    await File.AppendAllLinesAsync(resultingFilePath, new[] { name });
                }
            }
            catch (Exception exception)
            {
                Console.Write("Exception caught. Message: {0}", exception.Message);
            }
        }
    }

    private static async Task<Token?> GetToken()
    {
        var logins = await File.ReadAllLinesAsync("logins.txt");
        var random = new Random();
        var index = random.Next(0, logins.Length);
        var randomLogin = logins.ElementAt(index);

        var basicCredentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(randomLogin));
        Console.WriteLine(randomLogin.Split(":")[0]);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", basicCredentials) },
            RequestUri = new Uri("https://public-ubiservices.ubi.com/v3/profiles/sessions"),
            Content = new StringContent("{\"Content-Type\": \"application/json\"}", Encoding.UTF8,
                "application/json")
        };
        
        var response = await (await GetProxyHttpClient()).SendAsync(request);
        
        return response.IsSuccessStatusCode switch
        {
            true => JsonConvert.DeserializeObject<Token?>(await response.Content.ReadAsStringAsync())!,
            false => throw new ArgumentException(
                "Unable to retrieve Ubisoft Ticket. Make sure the login details are set to the correct value without any whitespace characters in front of it.")
        };
    }

    private static async Task<HttpClient> GetProxyHttpClient()
    {
        var proxies = await File.ReadAllLinesAsync("proxies.txt");

        if (proxies.Length == 0 || proxies[0] == "")
        {
            var proxyLessHttpClient = new HttpClient();
            proxyLessHttpClient.DefaultRequestHeaders.Clear();
            proxyLessHttpClient.DefaultRequestHeaders.Add("Ubi-AppId", "e3d5ea9e-50bd-43b7-88bf-39794f4e3d40");
            proxyLessHttpClient.DefaultRequestHeaders.Add("Ubi-RequestedPlatformType", "uplay");
            return proxyLessHttpClient;
        }

        var random = new Random();
        var index = random.Next(0, proxies.Length);
        Console.WriteLine(
            $"Url: {proxies.Select(proxy => proxy.Split("-")[0]).ElementAt(index).ToString()}\nUser:{proxies.Select(proxy => proxy.Split("-")[1]).ElementAt(index).ToString()}\nPassword:{proxies.Select(proxy => proxy.Split("-")[2]).ElementAt(index).ToString()}");
        var httpClientHandler = new HttpClientHandler
        {
            Proxy = new WebProxy
            {
                Address = new Uri(proxies.Select(proxy => proxy.Split("-")[0]).ElementAt(index).ToString()),
                BypassProxyOnLocal = false,
                Credentials =
                    new NetworkCredential(proxies.Select(proxy => proxy.Split("-")[1]).ElementAt(index).ToString(),
                        proxies.Select(proxy => proxy.Split("-")[2]).ElementAt(index).ToString())
            },
            UseProxy = true
        };
        var httpClient = new HttpClient(httpClientHandler);
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Ubi-AppId", "e3d5ea9e-50bd-43b7-88bf-39794f4e3d40");
        httpClient.DefaultRequestHeaders.Add("Ubi-RequestedPlatformType", "uplay");
        return httpClient;
    }
}
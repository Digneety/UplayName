using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NameCheck
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static UbisoftToken _token;

        public Program()
        {
            _httpClient.DefaultRequestHeaders.Add("Ubi-AppId", "2c2d31af-4ee4-4049-85dc-00dc74aef88f");
            _httpClient.DefaultRequestHeaders.Add("Ubi-RequestedPlatformType", "uplay");
            _httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 5_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B179 Safari/7534.48.3");
        }

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("   ");
            Console.WriteLine("   ");
            Console.WriteLine(" _   _                           ___    _                    _     ");
            Console.WriteLine("( ) ( )                         (  _`\\ ( )                  ( )    ");
            Console.WriteLine("| `\\| |   _ _   ___ ___     __  | ( (_)| |__     __     ___ | |/') ");
            Console.WriteLine("| , ` | /'_` )/' _ ` _ `\\ /'__`\\| |  _ |  _ `\\ /'__`\\ /'___)| , <  ");
            Console.WriteLine("| |`\\ |( (_| || ( ) ( ) |(  ___/| (_( )| | | |(  ___/( (___ | |\\`\\");
            Console.WriteLine("(_) (_)`\\__,_)(_) (_) (_)`\\____)(____/'(_) (_)`\\____)`\\____)(_) (_)");
            Console.WriteLine("    ");
            Console.WriteLine("                         Twitter: @Digneety                            ");
            Console.WriteLine("    ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("To stop checking names press ESC.");
            Console.ResetColor();
            do
            {
                while (!Console.KeyAvailable)
                {
                    CheckNamesAsync().GetAwaiter().GetResult();
                    break;
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        private static uint _namesChecked;
        private static uint _namesAvailable;
        private static uint _namesRestricted;

        private static async Task CheckNamesAsync()
        {
            if (_token == null || _token.IsExpired)
            {
                _token = await GetToken();
            }

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
                            new Uri($"https://public-ubiservices.ubi.com/v3/profiles?nameOnPlatform={name}&platformType=uplay"),
                        Headers = {Authorization = new AuthenticationHeaderValue("Ubi_v1", $"t={_token.Ticket}")}
                    };
                    var response = await _httpClient.SendAsync(request);

                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<JObject>(content);
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"Response from Ubisoft for name '{name}' - \n{content}");
                            CheckNamesAsync().Dispose();
                            await File.AppendAllTextAsync("failed.txt", content);
                        }
                    }

                    if (char.IsDigit(name[0]) || name.Contains("Ubi") || name.Contains("Ubisoft"))
                    {
                        _namesChecked += 1;
                        _namesRestricted += 1;
                        Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable} - Restricted: {_namesRestricted}";
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine($"{name}");
                        await File.AppendAllTextAsync("failed.txt", content);
                    }
                    else if (json != null && !json["profiles"]!.Any())
                    {
                        _namesChecked += 1;
                        _namesAvailable += 1;
                        Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable} - Restricted: {_namesRestricted}";
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{name}");
                        await File.AppendAllLinesAsync(resultingFilePath, new[] {name + "(Available or Restricted)"});
                    }

                    if (json != null && json["profiles"]!.Any())
                    {
                        _namesChecked += 1;
                        Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable} - Restricted: {_namesRestricted}";
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{name}");
                        Console.WriteLine($"{content}");
                    }
                } catch
                {
                    // ignored
                }
            }
        }

        private static async Task<UbisoftToken> GetToken()
        {
            _httpClient.DefaultRequestHeaders.Add("Ubi-AppId", "2c2d31af-4ee4-4049-85dc-00dc74aef88f");
            _httpClient.DefaultRequestHeaders.Add("Ubi-RequestedPlatformType", "uplay");
            _httpClient.DefaultRequestHeaders.Add("user-agent",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 5_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B179 Safari/7534.48.3");

            Console.WriteLine("Connecting to the Ubisoft Servers.");

            var basicCredentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes("chooseyourown@fucking.mail" + ":" + "andPassword"));

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Headers = {Authorization = new AuthenticationHeaderValue("Basic", basicCredentials)},
                RequestUri = new Uri("https://public-ubiservices.ubi.com/v3/profiles/sessions"),
                Content = new StringContent("{\"Content-Type\": \"application/json\"}", Encoding.UTF8,
                    "application/json")
            };
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new UnauthorizedAccessException("Unable to retrieve authentication ticket. Thanks ubisoft mf!");
            return JsonConvert.DeserializeObject<UbisoftToken>(await response.Content.ReadAsStringAsync());
        }
    }
}

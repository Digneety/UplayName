using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
        /**
         * Running the code.
         */
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
                    /**
                     * Sends the request to Ubisoft to check the name.
                     */
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri =
                             new Uri($"https://public-ubiservices.ubi.com/v3/profiles?nameOnPlatform={name}&platformType=uplay"),
                        Headers = { Authorization = new AuthenticationHeaderValue("Ubi_v1", $"t={_token.Ticket}") }
                    };
                    var response = await _httpClient.SendAsync(request);

                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<JObject>(content);
                    /**
                     * If we don't get the status code 200 back we know we are rate limited.
                     */
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"Response from Ubisoft for name '{name}' - \n{content}");
                            CheckNamesAsync().Dispose();
                            Console.ReadKey(true);
                            await File.AppendAllTextAsync("failed.txt", content);
                        }
                    }

                    /**
                     * To validate if the username is banned by Ubisoft. Gets Rate limited very fast so using it in this context is useless.
                     */
                    /**
                    var validationRequest = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri =
                            new Uri($"https://public-ubiservices.ubi.com/v3/profiles/{_token.profileId}/validateUpdate"),
                        Headers = { Authorization = new AuthenticationHeaderValue("Ubi_v1", $"t={_token.Ticket}") },
                        Content = new StringContent("{" + "\"nameOnPlatform\"" + ":" + "\"" + name + "\"" + "}", Encoding.UTF8, "application/json"),
                    };
                    var validationResponse = await _httpClient.SendAsync(validationRequest);
                    var validationResponseContent = validationResponse.Content.ReadAsStringAsync().Result;
                    //Console.WriteLine("{" + "\"nameOnPlatform\"" + ":" + "\"" + name + "\"" + "}");
                    if (validationResponseContent.Contains("ErrorCode") && !validationResponseContent.Contains("1011"))
                    {
                        _namesChecked += 1;
                        _namesRestricted += 1;
                        Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable} - Restricted: {_namesRestricted}";
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine($"{name}: {validationResponseContent}");
                        //await File.AppendAllTextAsync("failed.txt", content);
                    }
                   */

                    /**
                     * Checks if the names on the list contain one of these params which are blocked by default from Ubisoft.
                     */
                    if (char.IsDigit(name[0]) || name.Contains("Ubi") || name.Contains("Ubisoft"))
                    {
                        _namesChecked += 1;
                        _namesRestricted += 1;
                        Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable} - Restricted: {_namesRestricted}";
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine($"{name}");
                        await File.AppendAllTextAsync("failed.txt", content);
                    }
                    /**
                     * If the response we get when asking for the name does not contain the field profiles we know that no profile with this name exists.
                     */
                    else if (json != null && !json["profiles"]!.Any())
                    {
                        _namesChecked += 1;
                        _namesAvailable += 1;
                        Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable} - Restricted: {_namesRestricted}";
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{name}");
                        await File.AppendAllLinesAsync(resultingFilePath, new[] { name + "(Available or Restricted)" });
                    }
                    /**
                     * If it contains the field profiles we know that an Account with this name does already exist.
                     */
                    if (json != null && json["profiles"]!.Any())
                    {
                        _namesChecked += 1;
                        Console.Title = $"Checked: {_namesChecked} - Available: {_namesAvailable} - Restricted: {_namesRestricted}";
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{name}");
                    }
                    /**
                     * Limiting the requests so no rate limit occurs wait 2-5 minutes then it should be gone.
                     */
                    if (_namesChecked == 280)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("To prevent rate limiting the process will be terminated");
                        Console.ReadKey(true);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        /**
         * Retrieves the Authentication token needed for the requests above.
         */
        private static async Task<UbisoftToken> GetToken()
        {
            _httpClient.DefaultRequestHeaders.Add("Ubi-AppId", "afb4b43c-f1f7-41b7-bcef-a635d8c83822");
            _httpClient.DefaultRequestHeaders.Add("Ubi-RequestedPlatformType", "uplay");
            _httpClient.DefaultRequestHeaders.Add("user-agent",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 5_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B179 Safari/7534.48.3");

            Console.WriteLine("Connecting to the Ubisoft Servers.");

            var basicCredentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["mail"] + ":" + ConfigurationManager.AppSettings["password"]));
            /**
             * The request where we fetch the authentication token.
             */
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", basicCredentials) },
                RequestUri = new Uri("https://public-ubiservices.ubi.com/v3/profiles/sessions"),
                Content = new StringContent("{\"Content-Type\": \"application/json\"}", Encoding.UTF8,
                    "application/json")
            };
            var response = await _httpClient.SendAsync(request);
            /**
             * If Ubisoft changed the API or its values or if the account data is incorrect we get this exception.
             */
            if (!response.IsSuccessStatusCode)
                throw new UnauthorizedAccessException("Unable to retrieve authentication ticket.");
            return JsonConvert.DeserializeObject<UbisoftToken>(await response.Content.ReadAsStringAsync())!;
        }
    }
}
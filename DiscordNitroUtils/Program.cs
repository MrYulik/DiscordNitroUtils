using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Leaf.xNet;

namespace DiscordNitroUtils
{
    public enum ProxyType { Http = 1, Sosks4 = 2, Sosks5 = 3 }

    public class Program
    {
        private static List<string> _codes = new List<string>();
        private static List<string> _proxies = new List<string>();
        private static string _currentTime = DateTime.Now.ToString("dd-MM-yyyy hh-mm-ss");
        private static int _comboAmount;
        private static int _threads;
        private static string _webhookURL;
        private static ProxyType _currentProxy;
        private static ParallelLoopResult _parallelLoopResult;
        private static int _hitsCodes;
        private static int _invalidCodes;
        private static int _proxyError;
        private static int _rateLimit;
        private static int _checkedCodes;

        [STAThread]
        private static void Main(string[] args)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            Directory.CreateDirectory(Environment.CurrentDirectory + @"\\Logs\\");
            Console.Title = "Discord Nitro Utils by MrYulik";
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Discord Nitro Utils by MrYulik");
            Console.ForegroundColor = ConsoleColor.Red;
            CheckCode();
        }

        private static void CheckCode()
        {
            Console.Clear();
            Console.WriteLine("Discord Nitro Utils by MrYulik");
            Console.WriteLine("[DNU] Open codes file the proxy");
            Console.Write("> ");
            var openFile = new OpenFileDialog();
            openFile.Filter = "TEXT FILES (*.txt)|*.txt";
            openFile.Title = "Open file a codes";
            if(openFile.ShowDialog() == DialogResult.OK)
            {
                foreach (var code in File.ReadAllLines(openFile.FileName))
                {
                    _codes.Add(code);
                }
            }

            var openFile1 = new OpenFileDialog();
            openFile1.Filter = "TEXT FILES (*.txt)|*.txt";
            openFile1.Title = "Open file a proxy";
            if (openFile1.ShowDialog() == DialogResult.OK)
            {
                foreach (var code in File.ReadAllLines(openFile1.FileName))
                {
                    _proxies.Add(code);
                }
            }

            _comboAmount = _codes.Count;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"[DNU] Successfully inserted {_codes.Count} codes and {_proxies.Count} proxies: \n");
            
            Console.Write("[DNU] How many threads: \n");
            Console.Write("> ");
            _threads = int.Parse(Console.ReadLine());
            
            Console.Write("[DNU] Proxy type: [1] Http [2] Socks4 [3] Socks5 \n");
            Console.Write("> ");
            var proxy = int.Parse(Console.ReadLine());

            Console.Write("[DNU] Do you want to use discord webhooks? [1] Yes [2] No \n");
            Console.Write("> ");
            var chois = int.Parse(Console.ReadLine());
            if (chois == 1)
            {
                Console.Write("Enter your discord webhook URL \n");
                Console.Write("> ");
                var url = Console.ReadLine();
                _webhookURL = url;
            }

            _currentProxy = (ProxyType)proxy;
            Console.Clear();

            var thread = new Thread(SeparateThread);
            thread.IsBackground = true;
            Console.WriteLine("Discord Nitro Utils by MrYulik");
            thread.Start();

            while (_parallelLoopResult.IsCompleted != true)
            {
                if (_parallelLoopResult.IsCompleted)
                {
                    Console.Title = $"DiscordNitroUtils by MrYulik | Hits: {_hitsCodes} | Invalid: {_invalidCodes} | Proxy Error: {_proxyError} | Rate limited: {_rateLimit} | Remaining: {_comboAmount - _checkedCodes} ";
                    Console.WriteLine($"[!] Finished checking codes. Results: Hits: {_hitsCodes} | Invalid: {_invalidCodes} | Proxy Error: {_proxyError} | Rate limited: {_rateLimit} | Remaining: {_comboAmount - _checkedCodes}");
                    Console.ReadLine();
                }
            }
        }

        private static void SeparateThread()
        {
            _parallelLoopResult = Parallel.ForEach(_codes, code =>
            {
                new ParallelOptions { MaxDegreeOfParallelism = _threads };
                Check(code);
                Console.Title = $"DiscordNitroUtils by MrYulik | Hits: {_hitsCodes} | Invalid: {_invalidCodes} | Proxy Error: {_proxyError} | Rate limited: {_rateLimit} | Remaining: {_comboAmount - _checkedCodes} ";
            });
        }

        private static void Check(string code)
        {
            var url = "https://discordapp.com/api/v9/entitlements/gift-codes/" + code + "?with_application=false&with_subscription_plan=true";
            var httpRequest = new Leaf.xNet.HttpRequest();
            httpRequest.UserAgent = Http.ChromeUserAgent();
            switch(_currentProxy)
            {
                case ProxyType.Http:
                    httpRequest.Proxy = HttpProxyClient.Parse(RandomProxy());
                    httpRequest.Proxy.ConnectTimeout = 1000;
                    break;
                case ProxyType.Sosks4:
                    httpRequest.Proxy = Socks4ProxyClient.Parse(RandomProxy());
                    httpRequest.Proxy.ConnectTimeout = 1000;
                    break;
                case ProxyType.Sosks5:
                    httpRequest.Proxy = Socks5ProxyClient.Parse(RandomProxy());
                    httpRequest.Proxy.ConnectTimeout = 1000;
                    break;
            }

            try
            {
                var response = httpRequest.Get(url);
                if(response.ToString().Contains("redeemed"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[+] Hit:", code);
                    WriteFile(code);
                    _hitsCodes++;
                    _checkedCodes++;
                    if(_webhookURL != String.Empty)
                    {
                        SendWebhookMessage(_webhookURL, "DiscordNitroUtils", "!!Valid nitro hit!! discord.gift/" + code);
                    }
                }
            }
            catch(Leaf.xNet.HttpException e)
            {
                if(e.Message.Contains("404"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[-] Invalid:", code);
                    _invalidCodes++;
                    _checkedCodes++;
                }
                if(e.Message.Contains("429"))
                {
                    Console.WriteLine("[#] Rate limited: " + code);
                    Check(code);
                    _rateLimit += 1;
                }
                if (e.Status.Equals(HttpExceptionStatus.ConnectFailure))
                {
                    _proxyError++;
                    Check(code);
                }
            }
        }

        private static void SendWebhookMessage(string url, string username, string text)
        {
            var webClient = new WebClient();
            try
            {
                webClient.UploadValues(url, new NameValueCollection
                {
                    { "content", text },
                    { "username", username }
                });
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void WriteFile(string code)
        {
            var file = Environment.CurrentDirectory + @"\\Logs\\[Hits] " + _currentTime + ".txt";
            File.AppendAllText(file, code + Environment.NewLine);
        }

        private static string RandomProxy()
        {
            var random = new Random();
            var arrayProxy = _proxies.ToArray();
            var index = random.Next(arrayProxy.Length);
            return arrayProxy[index];
        }
    }
}

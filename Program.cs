using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using MingweiSamuel.Camille;
using YunoBot.Services;

namespace YunoBot
{
    class Program
    {
        private string CLIENT_ID;
        private string CLIENT_SECRET;
        private string BOT_TOKEN;
        private string RKEY;
        static private LogSeverity LogAt = LogSeverity.Info;


        //Services to maintain
        static private DiscordSocketClient main_client;
        private readonly IServiceCollection map = new ServiceCollection();
        private readonly CommandService commands = new CommandService();
        private RapiInfo rapi;
        
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private static Task Logger(LogMessage message){
        if (message.Severity > LogAt) return Task.CompletedTask;
        var cc = Console.ForegroundColor;
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                break;
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }
        Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,-8}] {message.Source}: {message.Message}");
        Console.ForegroundColor = cc;
        
        // If you get an error saying 'CompletedTask' doesn't exist,
        // your project is targeting .NET 4.5.2 or lower. You'll need
        // to adjust your project's target framework to 4.6 or higher
        // (instructions for this are easily Googled).
        // If you *need* to run on .NET 4.5 for compat/other reasons,
        // the alternative is to 'return Task.Delay(0);' instead.
        return Task.CompletedTask;
    }

        public async Task MainAsync(){
            using (StreamReader tfile = File.OpenText("config.json")){
                dynamic config = JsonConvert.DeserializeObject(tfile.ReadToEnd());
                CLIENT_ID = config.clientId ?? "NONE";
                CLIENT_SECRET = config.clientSecret ?? "NONE";
                RKEY = config.riotKey ?? throw (new ArgumentNullException("No Riot API Key (riotKey in config file) given!"));
                BOT_TOKEN = config.botToken ?? throw (new ArgumentNullException("No Discord Bot token (botKey in config file) given!"));
                
                
            }

            using (var services = ConfigServices()){
                main_client = services.GetRequiredService<DiscordSocketClient>();
                services.GetRequiredService<CommandService>().Log += Logger;
                rapi = services.GetRequiredService<RapiInfo>();
            
                main_client.Log += Logger;
                await main_client.LoginAsync(TokenType.Bot, BOT_TOKEN);
                await main_client.StartAsync();
                await services.GetRequiredService<Services.CommandHandlingService>().InitializeAsync();
                
                await Task.Delay(-1);
            }
        }


        private ServiceProvider ConfigServices(){
            

            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<String>(RKEY)
                .AddSingleton<RapiInfo>()
                .AddSingleton<CommandService>()
                .AddSingleton<Services.CommandHandlingService>()
                .BuildServiceProvider();
        }

     }
}

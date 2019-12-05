using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using MingweiSamuel.Camille;
using YunoBot.Services;

namespace YunoBot
{
    class YunoBot{
        private string CLIENT_ID;
        private string CLIENT_SECRET;
        private string BOT_TOKEN;
        private string RKEY;
        private string cacheFile;
        static private LogSeverity LogAt = LogSeverity.Info;


        //Services to maintain
        static private DiscordSocketClient main_client;
        private readonly IServiceCollection map = new ServiceCollection();
        private readonly CommandService commands = new CommandService();
        private RapiInfo rapi;

        
        public static Task Logger(LogMessage message){
            if (message.Severity > LogAt) return Task.CompletedTask;
            var cc = Console.ForegroundColor;
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
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
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,-8}] {message.Source, -15}| {message.Message}");
            Console.ForegroundColor = cc;
            
            return Task.CompletedTask;
        }
        
        public async Task MainAsync(){

            /* Read Config file and fill in appropriate variables */
            using (StreamReader tfile = File.OpenText("config.json")){
                dynamic config = JsonConvert.DeserializeObject(tfile.ReadToEnd());
                string tempPrefix = config.prefix ?? "`";
                int tempLevel = config.logLevel ?? 3;
                cacheFile = config.matchCacheFile ?? "matchCache.lol";

                CLIENT_ID = config.clientId ?? "NONE";
                CLIENT_SECRET = config.clientSecret ?? "NONE";
                RKEY = config.riotKey ?? throw (new ArgumentNullException("No Riot API Key (riotKey in config file) given!"));
                BOT_TOKEN = config.botToken ?? throw (new ArgumentNullException("No Discord Bot token (botKey in config file) given!"));
                CommandHandlingService.setLog(tempLevel);
                CommandHandlingService.setPrefix(tempPrefix);
            }
            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Info, "Config", $"Prefix set to:'{CommandHandlingService.Prefix}'"));
            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Info, "Config", $"Match cache file set to:'{cacheFile}'"));
            using (var services = ConfigServices()){
                main_client = services.GetRequiredService<DiscordSocketClient>();
                services.GetRequiredService<CommandService>().Log += Logger;
                rapi = services.GetRequiredService<RapiInfo>();

                rapi.setCacheFile(cacheFile);

                try{
                    await CommandHandlingService.Logger(new LogMessage(LogSeverity.Verbose, "Config", "Testing Riot API Key..."));
                    MingweiSamuel.Camille.SummonerV4.Summoner trialSummoner = rapi.RAPI.SummonerV4.GetBySummonerName(MingweiSamuel.Camille.Enums.Region.NA, "imaqtpie");
                    await CommandHandlingService.Logger(new LogMessage(LogSeverity.Verbose, "Config", "API Key passed"));
                }
                catch (AggregateException ex){
                    var respCode = (ex.InnerExceptions[0] as MingweiSamuel.Camille.Util.RiotResponseException).GetResponse().StatusCode;
                    await CommandHandlingService.Logger(new LogMessage(LogSeverity.Critical, "Config", $"Failed API Key test, response code: {(int)respCode}", ex));
                    switch(respCode){
                        case HttpStatusCode.Forbidden:
                            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Critical, "Config", $"API Key may be out of date! Try renewing the code at https://developer.riotgames.com/ "));
                            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Critical, "Config", $"Bot can function without Riot API, but all League of Legends data requesting will not work properly."));
                            break;
                        case HttpStatusCode.BadRequest:
                            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Critical, "Config", $"API key passed but tested Summoner did not exist. Possible problem with region", ex));
                            break;
                        default:
                            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Critical, "Config", $"Response name: {respCode.GetType().Name}"));
                            break;
                    }
                }
            
                main_client.Log += Logger;
                try {await main_client.LoginAsync(TokenType.Bot, BOT_TOKEN);}
                catch(Discord.Net.HttpException ex){
                    await Logger(new LogMessage(LogSeverity.Critical, "Config", "Could not login to Discord. Check token in config.json.", ex));
                    Environment.Exit(-1);                    
                }
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

        ~YunoBot(){
            rapi = null;
            Console.WriteLine($"{DateTime.Now} Deconstructed YunoBot");
        }
    };

    class Program
    {
        private static YunoBot running;
        
        static void Main(string[] args){
            running = new YunoBot();
            Console.CancelKeyPress += endhandler;
            Console.WriteLine($"{"Datetime",-19} [{"severity",-8}] {"source", -15}| {"message"}");
            running.MainAsync().GetAwaiter().GetResult();
        }

        private static void endhandler(object sender, ConsoleCancelEventArgs args){
            running = null;
            GC.Collect();
            Environment.Exit(1);
        }
     }
}

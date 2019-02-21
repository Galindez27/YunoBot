using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
namespace YunoBot
{
    class Program
    {
        private string CLIENT_ID;
        private string CLIENT_SECRET;
        private string BOT_TOKEN;
        private char COMM_PREFIX;
        private int COMM_PREF_POS = 0;

        static private DiscordSocketClient main_client;
        private readonly IServiceCollection map = new ServiceCollection();
        private readonly CommandService commands = new CommandService();
        private IServiceProvider services;

        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private static Task Logger(LogMessage message){
        var cc = Console.ForegroundColor;
        switch (message.Severity)
        {
            case LogSeverity.Critical:
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
            using (StreamReader tfile = File.OpenText("tokenfile")){
                while(!tfile.EndOfStream){
                    string line = tfile.ReadLine();
                    if(line == "botToken")
                        BOT_TOKEN = tfile.ReadLine();
                    else if (line == "clientSecret")
                        CLIENT_SECRET = tfile.ReadLine();
                    else if (line == "clientId")
                        CLIENT_ID = tfile.ReadLine();
                    else if (line == "prefix")
                        COMM_PREFIX = tfile.ReadLine()[0];
                }
            }
            main_client = new DiscordSocketClient(new DiscordSocketConfig{LogLevel = LogSeverity.Info});
            main_client.Log += Logger;
            // main_client.MessageReceived += MessageRecieved;
            await main_client.LoginAsync(TokenType.Bot, BOT_TOKEN);
            await initCommands();
            await main_client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task initCommands(){
            //TODO: add dependencies like Camille and anything else that comes up
            services = map.BuildServiceProvider();
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            main_client.MessageReceived += CommandHandler;
        }

        private async Task CommandHandler(SocketMessage message){
            var param = message as SocketUserMessage;
            if (param == null) return;
            
            await Logger(new LogMessage(LogSeverity.Debug, $"Message from ({message.Author})", message.Content));

            if (!(param.HasCharPrefix(COMM_PREFIX, ref COMM_PREF_POS))) return; //ignore any user message that doesnt have the command prefix or @Yuno_Bot

            var context = new CommandContext(main_client, param);
            var result = await commands.ExecuteAsync(context, COMM_PREF_POS, services); //Execute command
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
     }
}

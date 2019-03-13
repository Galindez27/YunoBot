using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System;
using System.IO;
using System.Reflection;

using Newtonsoft.Json;

using Microsoft.Extensions.DependencyInjection;

using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.LeagueV4;
using MingweiSamuel.Camille.MatchV4;

using YunoBot.Commands;

namespace YunoBot.Services{
    
    public class RapiInfo{
        public RiotApi RAPI;
        private string PatchNum = "00";
        

        private int _MaxWinrateNames = 1;
        private int _MaxRankedNames = 5;

        public string patchNum {get { return PatchNum;}}
        public int maxSearchRankedNames { get { return _MaxRankedNames;}}
        public int maxSearchWinrateNames { get { return _MaxWinrateNames;}}
        
        public RapiInfo(String key, IServiceProvider services){
            RAPI = RiotApi.NewInstance(key);
            updateLeaguePatch().GetAwaiter().GetResult();
        }

        

        public async Task updateLeaguePatch(){
            WebRequest temp = WebRequest.Create("https://ddragon.leagueoflegends.com/api/versions.json");
            WebResponse k = await temp.GetResponseAsync();
            Stream resp = k.GetResponseStream();
            StreamReader reader = new StreamReader(resp, System.Text.Encoding.GetEncoding("utf-8"));
            String jsonString = await reader.ReadToEndAsync();
            var json = JsonConvert.DeserializeObject<List<String>>(jsonString);
            lock(PatchNum){
                PatchNum = json[0];
            }
        }
    }

    public class CommandHandlingService{
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public readonly int maxSearchRankedNames;
        public readonly int maxSearchWinrateNames;

        private char prefix;
        private static LogSeverity LogAt = LogSeverity.Info;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            _commands.Log += Logger;
            _commands.CommandExecuted += CommandExecutedAsync;
            _discord.MessageReceived += MessageReceivedAsync;
        }

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

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        { //pulled from example code
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified){
                await Logger(new LogMessage(LogSeverity.Debug, "Command Execution", $"Command not found"));
                return;
            }

            await Logger(new LogMessage(LogSeverity.Debug, "Command Execution", $"Command executed: {command.Value.Name}"));
            // the command was succesful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess){
                await Logger(new LogMessage(LogSeverity.Debug, "Command Execution", "Successful"));
                return;
            }

            // the command failed, let's notify the user that something happened.
            await Logger(new LogMessage(LogSeverity.Debug, "Command Execution", $"Failure. Result: {result.ToString()}"));
            await context.Channel.SendMessageAsync($"error: {result.ToString()}");
        }

        public async Task InitializeAsync()
        { //pulled from example code
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            using (StreamReader tfile = File.OpenText("config.json")){
                dynamic config = JsonConvert.DeserializeObject(await tfile.ReadToEndAsync());
                prefix = config.prefix ?? (char)8;
            }
            if (prefix == (char)8){
                await Logger(new LogMessage(LogSeverity.Warning, "Configuraton", "No prefix found in config.json! Default is '`'"));
                prefix = '`';
            }
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {//modified from example code
            var argPos = 0;
            await Logger(new LogMessage(LogSeverity.Debug, "Message Recieved", rawMessage.ToString()));
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            if (message.HasCharPrefix(prefix, ref argPos)){
                var context = new SocketCommandContext(_discord, message);
                await _commands.ExecuteAsync(context, argPos, _services); // we will handle the result in CommandExecutedAsync
                return;
            }
            argPos = 0;
            if (message.HasMentionPrefix(_discord.CurrentUser, ref argPos)){
                var context = new SocketCommandContext(_discord, message);
                await _commands.ExecuteAsync(context, argPos, _services); // we will handle the result in CommandExecutedAsync
                return;
            }
        }
    }
}
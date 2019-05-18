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
    public class CommandHandlingService{
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public DiscordSocketClient discordClient {get {return _discord;}}

        public readonly int maxSearchRankedNames;
        public readonly int maxSearchWinrateNames;
        public readonly string HelpMessage;

        private static LogSeverity LogAt = LogSeverity.Info;
        private static string prefix;
        private static Embed _groupHelpMessage;
        private static Dictionary<string, Embed> _helpMessages;
        private static List<ModuleInfo> _loadedModules;

        public static uint embedColor = 0xff69b4;

        public static List<ModuleInfo> LoadedModule {get {return _loadedModules;}}
        public static string Prefix {get { return prefix;}}
        public static Embed GroupHelpMessage {get { return _groupHelpMessage;}}

        public static readonly string MainHelpText = "Listed below with :small_red_triangle: are groups. You will also find a small summary of each group, aliases to quickly call them, and commands you can activate.\n\nTo interact with me, you type \n*{0}<group> <command> <arguments.>*\n\n For Example, you can type: *{1}search rank \"imaqtpie\"* to lookup the best AD in NA!\n\nIf you cannot see the list below, note that this bot requires the embed permission to function properly!";

        public CommandHandlingService(IServiceProvider services){
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            _commands.Log += Logger;
            _commands.CommandExecuted += CommandExecutedAsync;
            _discord.MessageReceived += MessageReceivedAsync;            
        }
        
        public static void setLog(int newLevel){
            LogAt = (LogSeverity)newLevel;
        }
        public bool getHelp(string groupName, out Embed helpEmbed){
            // Returns true if there is a help embed by the specified name, else false.
            return _helpMessages.TryGetValue(groupName, out helpEmbed);
        }
        public void generateHelp(){
            // Generate Help Messages for Modules and Commands
            Logger(new LogMessage(LogSeverity.Verbose, "Help Command", "Generating Help Embeds"));
            EmbedAuthorBuilder me = new EmbedAuthorBuilder();
            while (_discord.CurrentUser == null){}
            me.WithIconUrl(_discord.CurrentUser.GetAvatarUrl());
            me.Name = _discord.CurrentUser.Username;
            
            EmbedBuilder mainHelpMessage = new EmbedBuilder();
            List<EmbedFieldBuilder> mainHelpEmbedFields = new List<EmbedFieldBuilder>();
            Dictionary<string, Embed> groupHelpMessages = new Dictionary<string, Embed>();

            foreach (ModuleInfo modInfo in _loadedModules){
                CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "Help Command", $"Module Read:{modInfo.Name}"));

                EmbedFieldBuilder mainGroupField = new EmbedFieldBuilder();
                EmbedBuilder soloGroupHelp = new EmbedBuilder();
                List<EmbedFieldBuilder> soloGroupCommands = new List<EmbedFieldBuilder>();
                string embedFieldForm = "**Summary**\n{0}\n\n**Aliases**\n{1}\n\n**{2}**\n{3}";
                string summ = "";
                string ali = "";
                string flex = "";
                string flexv = "";

                mainGroupField.IsInline = true;
                mainGroupField.Name = ":small_red_triangle:" + modInfo.Name;
                soloGroupHelp.Title = modInfo.Name.ToLower();

                string[] summSplit = modInfo.Summary.Split(' ');
                int lineLen = 0;
                foreach (string word in summSplit){ // Wrap text to 31 characters
                    if (lineLen + word.Length > 31){
                        lineLen = 0;
                        summ += '\n';
                    }
                    summ += word + ' ';
                    lineLen += word.Length + 1;
                }

                if (modInfo.Aliases.Count == 0) { ali += "None";}
                else {
                    foreach (string alias in modInfo.Aliases){
                        ali += alias + " ";
                    }
                }

                flex = "Commands";
                foreach (CommandInfo comm in modInfo.Commands){ // Iterate through commands of each group
                    string soloSumm = "None";
                    string soloAli = "None";
                    string soloFlex = "Arguments";
                    string soloFlexV = comm.Remarks ?? "None";
                    int numAli = comm.Aliases.Count / modInfo.Aliases.Count;

                    flexv += $"{comm.Name} ";
                    EmbedFieldBuilder commandField = new EmbedFieldBuilder();
                    commandField.Name = ":speech_balloon:" + comm.Name;
                    commandField.IsInline = true;

                    int linelen = 0;
                    if (comm.Summary != null){
                        soloSumm = "";
                        foreach (string word in comm.Summary.Split(' ')){ // Wrap text to 31 characters
                            if (linelen + word.Length > 31){
                                soloSumm += '\n';
                                linelen = 0;
                            }
                            soloSumm += word + ' ';
                            linelen += word.Length + 1;
                        }
                    }
                    if (numAli != 1) {
                        soloAli = "";
                        for (int i = 1; i < numAli; i++){
                            soloAli += comm.Aliases[i].Substring(modInfo.Aliases[0].Length) + " ";
                        }
                    }

                    commandField.Value = string.Format(embedFieldForm, soloSumm, soloAli, soloFlex, soloFlexV);
                    soloGroupCommands.Add(commandField);
                }
                soloGroupHelp.WithFields(soloGroupCommands);
                soloGroupHelp.WithColor(0xff69b4);
                soloGroupHelp.WithCurrentTimestamp();
                soloGroupHelp.WithFooter(new EmbedFooterBuilder().WithText("Live since: "));
                soloGroupHelp.WithAuthor(me);
                groupHelpMessages.Add(soloGroupHelp.Title, soloGroupHelp.Build());

                mainGroupField.Value = string.Format(embedFieldForm, summ, ali, flex, flexv);
                mainHelpEmbedFields.Add(mainGroupField);
            }
            

            mainHelpMessage.WithColor(0xff69b4);
            mainHelpMessage.WithCurrentTimestamp();
            mainHelpMessage.WithTitle("GitHub");
            mainHelpMessage.WithUrl("https://github.com/Galindez27/YunoBot");
            mainHelpMessage.WithFields(mainHelpEmbedFields);
            mainHelpMessage.WithFooter(new EmbedFooterBuilder().WithText("Live since: "));
            mainHelpMessage.WithAuthor(me);
            _groupHelpMessage = mainHelpMessage.Build();
            _helpMessages = groupHelpMessages;
        }

        public static Task Logger(LogMessage message){
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
        Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,-8}] {message.Source, -15}| {message.Message}");
        Console.ForegroundColor = cc;
        
        // If you get an error saying 'CompletedTask' doesn't exist,
        // your project is targeting .NET 4.5.2 or lower. You'll need
        // to adjust your project's target framework to 4.6 or higher
        // (instructions for this are easily Googled).
        // If you *need* to run on .NET 4.5 for compat/other reasons,
        // the alternative is to 'return Task.Delay(0);' instead.
        return Task.CompletedTask;
    }

        public static void setPrefix(string newPrefix){
            prefix = newPrefix;
        }
        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        { //pulled from example code
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified){
                await Logger(new LogMessage(LogSeverity.Verbose, "Comm Execution", $"Command not found"));
                await context.User.SendMessageAsync($":confounded: Command not found!");
                return;
            }

            await Logger(new LogMessage(LogSeverity.Verbose, "Comm Execution", $"Command executed: {command.Value.Name}"));
            // the command was succesful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess){
                await Logger(new LogMessage(LogSeverity.Debug, "Comm Execution", "Successful"));
                return;
            }

            // the command failed, let's notify the user that something happened.
            await Logger(new LogMessage(LogSeverity.Error, "Comm Execution", $"Failure. Result: {result}"));
            // await Logger(new LogMessage(LogSeverity.Error, "Comm Execution", $"{result.Error.Value}"))
            await context.User.SendMessageAsync($"Something went wrong!\nReason:{result.ErrorReason}");
        }

        public async Task InitializeAsync()
        { //pulled from example code
            _loadedModules = new List<ModuleInfo>(await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services));
            generateHelp();
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage){//modified from example code
            var argPos = 0;
            await Logger(new LogMessage(LogSeverity.Verbose, "MessageRecieved", $"Author:{rawMessage.Author}"));
            if (!rawMessage.Author.IsBot) {await Logger(new LogMessage(LogSeverity.Verbose, "MessageRecieved", rawMessage.ToString()));}
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            if (message.HasStringPrefix(prefix, ref argPos)){
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
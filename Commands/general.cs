using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System;
using System.IO;
using System.Reflection;

using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.LeagueV4;
using MingweiSamuel.Camille.MatchV4;
using MingweiSamuel.Camille.Util;
using MingweiSamuel.Camille.SpectatorV4;

using YunoBot.Services; 

namespace YunoBot.Commands{
    [Name("General")]
    [Summary("These commands do not require the <group> argument.")]
    public class General : ModuleBase<SocketCommandContext>{
        private string[] uwus = {"𝓤𝔀𝓤", "ÚwÚ", "(。U ω U。)", "(⁄˘⁄ ⁄ ω⁄ ⁄ ˘⁄)♡", "end my suffering",
         "✧･ﾟ: *✧･ﾟ♡*(ᵘʷᵘ)*♡･ﾟ✧*:･ﾟ✧", "𝒪𝓌𝒪", "(⁄ʘ⁄ ⁄ ω⁄ ⁄ ʘ⁄)♡", "uwu"};
        private CommandHandlingService _handler;
        RapiInfo _rapi;

        public General(CommandHandlingService handlingService, RapiInfo rapi){
            _handler = handlingService;
            _rapi = rapi;
        }

        [Command("hello"), Summary("Say hello. Simple Ping"), Priority(1)]
        public async Task Say([Remainder()]string remainder ){
            await ReplyAsync($"Hello! {Context.User.Username} said: {remainder}");
        }

        [Command("hello"), Summary("Say hello."), Priority(0)]
        public async Task Say(){
            await ReplyAsync("Hello!");
        }

        [Command("help"), Alias("h", "?", "pls", "wtf", "halp"), Summary("Reply with some helpful info!")]
        public async Task yunoHelp(){
            Embed toEmbed = CommandHandlingService.GroupHelpMessage;
            await Context.User.SendMessageAsync($"Listed below with :small_red_triangle: are groups. You will also find a small summary of each group, aliases to quickly call them, and commands you can activate.\n\nTo interact with me, you type \n*\\{CommandHandlingService.Prefix}<group> <command> <arguments.>*\n\n For Example, you can type: *\\{CommandHandlingService.Prefix}search rank \"imaqtpie\"* to lookup the best AD in NA!\n\nIf you cannot see the list below, note that this bot requires the embed permission to function properly!", embed:toEmbed);
        }

        [Command("help"), Alias("h", "?", "pls", "wtf", "halp"), Summary("Provide detailed help for a group and commands"), Remarks("<group name>")]
        public async Task yunoHelpGroup([Remainder()]string remainder){
            Embed helpMsg = null;
            if (_handler.getHelp(remainder.ToLower(), out helpMsg)){
                await Context.User.SendMessageAsync(embed:helpMsg);
            }
            else{
                await Context.User.SendMessageAsync($"Group: {remainder} not found.");
            }
        }

        [Command("uwu"), Summary("*uwu*")]
        public async Task degenerecy(){
            Random rng = new Random();
            await ReplyAsync($"{uwus[rng.Next(uwus.Length)]}");
        }
    }
}
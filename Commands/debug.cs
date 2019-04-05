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
    [Group("Debug"), RequireOwner(), Name("debug"), Alias("db")]
    [Summary("Bot debug and service commands. Can only be invoked by my owner.")]
    public class DebugCommands : ModuleBase<SocketCommandContext>{
        public RapiInfo _rapi;


        private Dictionary<string, Object> allServices;
        public DebugCommands(RapiInfo rapi, CommandHandlingService handlerService){
            allServices = new Dictionary<string, object>();
            allServices.Add("rapi", rapi); _rapi = rapi;
            allServices.Add("commandHandler", handlerService);
        }

        [Command("updateLeague")]
        public async Task upLeague(){
            RapiInfo rapi = allServices["rapi"] as RapiInfo;
            await Context.Channel.SendMessageAsync($"Current patch: {rapi.patchNum}");
            await rapi.updateLeaguePatch();
            await ReplyAsync($"New patch: {rapi.patchNum}");
        }

        [Command("allServices")]
        public async Task basicDebug(){
            string rep = "```";
            foreach (var v in allServices){
                rep += v.GetType() + "\n\t";
                foreach (var prop in v.GetType().GetProperties()){
                    rep += $"{prop.Name} : {prop.GetValue(v)}\n\t";
                }
                rep += "\n"; 
            }
            rep+= "```";
            await ReplyAsync(rep);
        }

        [Command("halt"), Alias("s")]
        public Task halt(){
            _rapi.dumpCache();
            Environment.Exit(1);
            return Task.CompletedTask;
        }
    }
}
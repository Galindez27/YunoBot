using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System;
using System.IO;

using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.LeagueV4;
using MingweiSamuel.Camille.MatchV4;
using MingweiSamuel.Camille.Util;

using YunoBot.Services; 

namespace YunoBot.Commands{
    [Summary("General Commands")]
    public class General : ModuleBase<SocketCommandContext>{
        [Command("hello"), Summary("Say hello. Simple Ping"), Priority(1)]
        public async Task Say([Remainder()]string remainder ){
            await ReplyAsync($"Hello! {Context.User.Username} said: {remainder}");
        }

        [Command("hello"), Summary("Say hello2"), Priority(0)]
        public async Task Say(){
            await ReplyAsync("Hello!");
        }

        
    }

    [Group("search"), Summary("Search for information")]
    public class Search : ModuleBase<SocketCommandContext>{

        private RapiInfo _rapi;

        public Search(RapiInfo rpi){
            _rapi = rpi;
        }

        private async Task<bool> isWin(long game, Summoner tocheck){
            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "isWin Check", $"GameID:{game}, PlayerACC:{tocheck.AccountId}, Name:{tocheck.Name}"));
            Match match = await _rapi.RAPI.MatchV4.GetMatchAsync(Region.NA, game);
            int playerId = -1;
            int playerTeam = -1;
            int playerTeamId = -1;
            foreach (var part in match.ParticipantIdentities){
                if (part.Player.CurrentAccountId == tocheck.AccountId){
                    playerId = part.ParticipantId - 1;
                    playerTeamId = match.Participants[playerId].TeamId;
                    playerTeam = (playerTeamId == 100) ? 0 : 1;
                    break;
                }
            }
            if (match.Teams[playerTeam].TeamId != playerTeamId){
                throw new IndexOutOfRangeException($"TeamId found:{playerTeamId}, Team index made:{playerTeam}, TeamId in index reached:{match.Teams[playerTeam].TeamId}");
            }
            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "isWinCheck", $"{match.Teams[playerTeam].Win == "Win"}"));
            return match.Teams[playerTeam].Win == "Win";
        }
        
        [Command("rank"), Summary("Search for summoner ranks by name")]
        public async Task byname(params string[] names){
            if (names.Length > _rapi.maxSearchRankedNames){ 
                await ReplyAsync($"Too many names! Max of {_rapi.maxSearchRankedNames}.");
                return;
                }
            await Context.Channel.TriggerTypingAsync();
            
            List<Summoner> targets = new List<Summoner>();
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            foreach (string name in names){
                EmbedFieldBuilder toAdd = new EmbedFieldBuilder();
                toAdd.Name = name;
                toAdd.Value = " ";

                // Make sure the summoner name exists and has ranks before adding to fields
                try {
                    Summoner targSumm = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, name);
                    targets.Add(targSumm);
                    LeaguePosition[] positions = await _rapi.RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, targSumm.Id);
                    foreach (var pos in positions){
                        string queue = pos.QueueType == "RANKED_FLEX_SR" ? "Flex" : pos.QueueType == "RANKED_SOLO_5x5" ? "Solo/Duo" : pos.QueueType == "RANKED_FLEX_TT" ? "3v3" : pos.QueueType;
                        string val = string.Format("{0}: {1} {2}\n", queue, (pos.Tier[0] + pos.Tier.Substring(1).ToLower()), pos.Rank);
                        toAdd.Value += val;
                    }
                }
                catch (RiotResponseException respError){
                    if (respError.GetResponse().StatusCode == HttpStatusCode.NotFound){
                        toAdd.Value = "Does not exist";
                    }
                }
                fields.Add(toAdd);
            }
            
            int ProfileIconId;
            try {ProfileIconId = targets[0].ProfileIconId;}
            catch (NullReferenceException){ProfileIconId = 501;}

            EmbedBuilder embeddedMessage = new EmbedBuilder();
            embeddedMessage.WithTitle("");
            embeddedMessage.WithThumbnailUrl($"http://ddragon.leagueoflegends.com/cdn/{_rapi.patchNum}/img/profileicon/{ProfileIconId}.png");
            embeddedMessage.WithColor(0xff69b4);
            embeddedMessage.WithFields(fields);
            embeddedMessage.WithCurrentTimestamp();
            //embeddedMessage.WithFooter("As of");
            await ReplyAsync("", embed:embeddedMessage.Build());
        }
        
        [Command("winrate"), Summary("Get a last 20 Ranked games winrate for a player")]
        public async Task wr(params string[] names){
            if(names.Length > _rapi.maxSearchWinrateNames)
                await Context.User.SendMessageAsync($"Too many names entered! Limit: {_rapi.maxSearchWinrateNames}");
            else{
                int[] qs = {420, 440};
                double wins = 0, losses = 0;
                await Context.Channel.TriggerTypingAsync();
                Summoner tofind = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, names[0]);
                Matchlist mlist = await _rapi.RAPI.MatchV4.GetMatchlistAsync(Region.NA, tofind.AccountId, queue:qs, endIndex:20);
                foreach (MatchReference mref in mlist.Matches){
                    if (await isWin(mref.GameId, tofind)) { wins++ ;}
                    else { losses++; }
                }
                
                double wr  = wins / (wins + losses);
                List<EmbedFieldBuilder> content = new List<EmbedFieldBuilder>();
                EmbedFieldBuilder first = new EmbedFieldBuilder();
                first.Name = $"Wins: {(int)wins}";
                first.Value = " ";
                EmbedFieldBuilder second = new EmbedFieldBuilder();
                second.Name = $"Losses: {(int)losses}";
                second.Value = " ";
                EmbedFieldBuilder third = new EmbedFieldBuilder();
                third.Name = $"Winrate: {wr:P}";
                third.Value = " ";

                content.Add(first);
                content.Add(second);
                content.Add(third);

                EmbedBuilder embeddedMessage = new EmbedBuilder();
                embeddedMessage.WithThumbnailUrl($"http://ddragon.leagueoflegends.com/cdn/{_rapi.patchNum}/img/profileicon/{tofind.ProfileIconId}.png");
                embeddedMessage.WithColor(0xff69b4);
                embeddedMessage.WithTitle("Last Twenty All Ranked Queues");
                embeddedMessage.WithAuthor(tofind.Name);
                embeddedMessage.WithColor(0xff69b4);
                embeddedMessage.WithFields(content);
                embeddedMessage.WithCurrentTimestamp();
                embeddedMessage.WithFooter(new EmbedFooterBuilder().WithText("As of"));
                await ReplyAsync("", embed:embeddedMessage.Build());
            }
            
        }
    }

    [Group("admin"), RequireUserPermission(GuildPermission.Administrator)]
    public class adminCommands : ModuleBase<SocketCommandContext>{
        
        [Command("list"), Summary("list the admins and lieutenants of a server")]
        public async Task listInfo(){
            await Context.Guild.DownloadUsersAsync();
            HashSet<SocketRole> roles = new HashSet<SocketRole>(Context.Guild.Roles);
            HashSet<SocketRole> nImportant = new HashSet<SocketRole>();
            foreach (SocketRole r in roles){
                if (r.Permissions.Administrator || r.Permissions.BanMembers || r.Permissions.ManageGuild
                || r.Permissions.ManageRoles || r.Permissions.ManageWebhooks || r.Permissions.SendTTSMessages
                || r.Permissions.DeafenMembers || r.Permissions.MentionEveryone){
                    nImportant.Add(r);
                } 
            }
            EmbedBuilder toEmbed = new EmbedBuilder();
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            toEmbed.WithColor(0xff69b4);
            toEmbed.WithTitle(Context.Guild.Name + "\nImportant People");
            toEmbed.ThumbnailUrl = Context.Guild.IconUrl;
            
            foreach (var user in Context.Guild.Users){
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                bool isImportant = false;
                field.Name = user.Username;
                field.Value = "```";
                foreach (var role in user.Roles){
                    if (nImportant.Contains(role)){
                        isImportant = true;
                    }
                    if (!role.IsEveryone) field.Value += role.Name + ", ";
                }
                field.Value += "```";
                if (isImportant){
                    fields.Add(field);
                }
            }
            toEmbed.WithFields(fields);
            
            await ReplyAsync(embed:toEmbed.Build());
        }
    
    }

    [Group("Debug"), RequireOwner()]
    public class DebugCommands : ModuleBase<SocketCommandContext>{
        private Dictionary<string, Object> allServices;
        public DebugCommands(RapiInfo rapi, CommandHandlingService handlerService){
            allServices = new Dictionary<string, object>();
            allServices.Add("rapi", rapi);
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
    }
}
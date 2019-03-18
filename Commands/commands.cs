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

using YunoBot.Services; 

namespace YunoBot.Commands{
    [Summary("General Commands\nThese commands do not\nrequire the <group> argument."), Name("General Commands.")]
    public class General : ModuleBase<SocketCommandContext>{
        private string[] uwus = {"𝓤𝔀𝓤", "ÚwÚ", "(。U ω U。)", "(⁄˘⁄ ⁄ ω⁄ ⁄ ˘⁄)♡", "end my suffering", "✧･ﾟ: *✧･ﾟ♡*(ᵘʷᵘ)*♡･ﾟ✧*:･ﾟ✧", "𝒪𝓌𝒪", "(⁄ʘ⁄ ⁄ ω⁄ ⁄ ʘ⁄)♡"};
        private CommandHandlingService _handler;


        public General(CommandHandlingService handlingService){
            _handler = handlingService;
        }

        [Command("hello"), Summary("Say hello. Simple Ping"), Priority(1)]
        public async Task Say([Remainder()]string remainder ){
            await ReplyAsync($"Hello! {Context.User.Username} said: {remainder}");
        }

        [Command("hello"), Summary("Say hello2"), Priority(0)]
        public async Task Say(){
            await ReplyAsync("Hello!");
        }

        [Command("help"), Alias("h", "?", "pls", "wtf", "halp"), Summary("Reply with some helpful info!")]
        public async Task yunoHelp(){
            EmbedAuthorBuilder me = new EmbedAuthorBuilder();
            me.WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
            me.Name = Context.Client.CurrentUser.Username;

            EmbedBuilder toEmbed = CommandHandlingService.GroupHelpMessage;
            toEmbed.WithAuthor(me);
            await ReplyAsync($"Listed below with :black_small_square: are groups. You will also find a small summary of each group, aliases to quickly call them, and commands you can activate.\n\nTo interact with me, you type \n*\\{CommandHandlingService.Prefix}<group> <command> <arguments.>*\n\n For Example, you can type: *\\{CommandHandlingService.Prefix}search rank \"Yandere Supreme\"* to lookup my creator's league rank!\n\nIf you cannot see the list below, note that this bot requires the embed permission to function properly!", embed:toEmbed.Build());
        }
    
        [Command("uwu"), Summary("*uwu*")]
        public async Task degenerecy(){
            Random rng = new Random();
            await ReplyAsync($"{uwus[rng.Next(uwus.Length)]}");
        }
    }

    [Group("search"), Summary("Search for LoL related information."), Alias("s"), Name("Search Commands")]
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
            //await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "isWinCheck", $"{match.Teams[playerTeam].Win == "Win"}"));
            return match.Teams[playerTeam].Win == "Win";
        }
        
        [Command("rank"), Summary("Search for summoner ranks by name"), Alias("player", "summoner", "r")]
        public async Task byname(params string[] names){
            if (names.Length > _rapi.maxSearchRankedNames){ 
                await ReplyAsync($"Too many names! Max of {_rapi.maxSearchRankedNames}.");
                return;
                }
            await Context.Channel.TriggerTypingAsync();

            EmbedBuilder toEmbed = new EmbedBuilder();
            List<EmbedFieldBuilder> fieldList = new List<EmbedFieldBuilder>();
            Summoner topSumm = null;

            foreach (string target in names){
                EmbedFieldBuilder tempField = new EmbedFieldBuilder();
                tempField.Name = target;
                tempField.Value = " ";
                tempField.IsInline = true;

                Summoner summTarget = null;
                LeaguePosition[] positions = null; 
                try {
                    summTarget = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException();
                    topSumm = topSumm ?? summTarget;
                    positions = await _rapi.RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, summTarget.Id);
                    foreach (LeaguePosition pos in positions){
                        string queue = pos.QueueType == "RANKED_SOLO_5x5" ? "Solo/Duo" :
                                        pos.QueueType == "RANKED_FLEX_SR" ? "Flex" :
                                        pos.QueueType == "RANKED_FLEX_TT" ? "Treeline" : (pos.QueueType[0] + pos.QueueType.Substring(1).ToLower());
                        tempField.Value += $"{queue}: {pos.Tier[0] + pos.Tier.Substring(1).ToLower()} {pos.Rank}\n";
                    }
                }
                catch (InvalidDataException){
                    tempField.Value = "Does not exist";
                }
                fieldList.Add(tempField);
            }
            
            toEmbed.ThumbnailUrl = $"http://ddragon.leagueoflegends.com/cdn/{_rapi.patchNum}/img/profileicon/{(topSumm != null ? topSumm.ProfileIconId : 501)}.png";
            toEmbed.WithColor(0xff69b4);
            toEmbed.WithCurrentTimestamp();
            toEmbed.WithFields(fieldList);
            await ReplyAsync(embed:toEmbed.Build());
        }
        
        [Command("winrate"), Summary("Get a last 20 Ranked games winrate for a player"), Alias("wr")]
        public async Task wr(params string[] names){
            if(names.Length > _rapi.maxSearchWinrateNames)
                await Context.User.SendMessageAsync($"Too many names entered! Limit: {_rapi.maxSearchWinrateNames}");
            else{                
                await Context.Channel.TriggerTypingAsync();
                
                List<EmbedFieldBuilder> content = new List<EmbedFieldBuilder>();
                Summoner topSumm = null;
                int[] qs = {420, 440};

                foreach (string target in names){
                    EmbedFieldBuilder field = new EmbedFieldBuilder();
                    field.Name = target;

                    Summoner tofind = null;
                    Matchlist mlist = null;
                    double wins = 0, losses = 0, wr;
                    try{
                        tofind = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException();
                        mlist = await _rapi.RAPI.MatchV4.GetMatchlistAsync(Region.NA, tofind.AccountId, queue:qs, endIndex:20);
                        if (mlist.TotalGames == 0) {throw new InvalidOperationException();}
                        
                        topSumm = topSumm ?? tofind;

                        foreach (MatchReference mref in mlist.Matches){
                            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "winrate", $"mref: {(mref != null)}"));
                            if (await isWin(mref.GameId, tofind)) {wins++;}
                            else {losses++;}
                        }
                        wr = wins / (wins + losses);
                        field.Value = string.Format("WR: {0:P}\nWins: {1}\nLosses:{2}\n", wr, wins, losses);
                    }
                    catch (InvalidDataException){
                        field.Value = "Does not exist";
                    }
                    catch (InvalidOperationException){
                        field.Value = "No ranked games on record";
                    }
                    catch (NullReferenceException nre){
                        await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "winrate", "Poorly Handled Exception", nre));
                        field.Value = "Error";
                        topSumm = tofind ?? tofind ?? topSumm;
                    }

                    content.Add(field);
                }
                await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "winrate command", "Building embedded message..."));
                EmbedBuilder embeddedMessage = new EmbedBuilder();
                embeddedMessage.WithThumbnailUrl($"http://ddragon.leagueoflegends.com/cdn/{_rapi.patchNum}/img/profileicon/{(topSumm != null ? topSumm.ProfileIconId : 501)}.png");
                embeddedMessage.WithColor(0xff69b4);
                embeddedMessage.WithTitle("Last Twenty All Ranked Queues");
                embeddedMessage.WithColor(0xff69b4);
                embeddedMessage.WithFields(content);
                embeddedMessage.WithCurrentTimestamp();
                embeddedMessage.WithFooter(new EmbedFooterBuilder().WithText("As of"));
                await ReplyAsync("", embed:embeddedMessage.Build());
            }
            
        }
    }

    [Group("admin"), RequireUserPermission(GuildPermission.Administrator),  Name("Admin Commands"), Summary("Server admin commands.\nCan only be called by admins\nof a server.")]
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

    [Group("Debug"), RequireOwner(), Name("Debug Commands"), Summary("Bot debug and service commands.\nCan only be invoked by my owner.")]
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
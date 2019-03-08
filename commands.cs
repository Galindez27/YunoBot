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

using YunoBot.Services;

namespace YunoBot.Commands{
    public class General : ModuleBase<SocketCommandContext>{
        [Command("hello"), Summary("Say hello. Simple Ping")]
        public async Task Say(params string[] remainder){
            string tobuild = "";
            foreach (string g in remainder){
                tobuild += g + " ";
            }
            await ReplyAsync($"Hello! {Context.User.Username} said: {tobuild}");
        }

        [Command("hello"), Summary("Say hello2")]
        public async Task Say(){
            await ReplyAsync("Hello!");
        }

        
    }

    [Group("search"), Summary("Search for information")]
    public class Search : ModuleBase<SocketCommandContext>{

        public RapiInfo RapiInfoService {get; set;}
        private int maxNamesSummoner = 5;
        private int maxNamesWinrate = 1;

        public async Task<bool> isWin(long game, Summoner tocheck){
            Match match = await RapiInfoService.RAPI.MatchV4.GetMatchAsync(Region.NA, game);
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
            return match.Teams[playerTeam].Win == "Win";
        }

        [Command("rank"), Summary("Search for summoner ranks by name")]
        public async Task byname(params string[] names){
            if (names.Length > maxNamesSummoner){ 
                await ReplyAsync($"Too many names! Max of {maxNamesSummoner}.");
                return;
                }
            await Context.Channel.TriggerTypingAsync();
            List<Summoner> toprint = new List<Summoner>();
            List<EmbedFieldBuilder> fieldsX = new List<EmbedFieldBuilder>();
            foreach (string n in names){
                try{
                    EmbedFieldBuilder field = new EmbedFieldBuilder();
                    Summoner summ = await RapiInfoService.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, n);
                    LeaguePosition[] leagueInfo = await RapiInfoService.RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, summ.Id);

                    toprint.Add(summ);
                    field.WithName(summ.Name);
                    string temp = "";
                    foreach(var t in leagueInfo){
                        if (t.Position != "NONE" && t.Position != "APEX"){
                            temp += $"{(t.Position == "UTILITY" ? "Support" : (t.Position[0])+t.Position.Substring(1).ToLower())} - {t.Tier[0]+t.Tier.Substring(1).ToLower() , 7} {t.Rank}\n";
                            //temp += $" WR: {(t.Wins / (t.Wins + t.Losses))*100}%\n";
                        }
                        else if (t.Position == "APEX"){
                            temp += $"Solo Queue - {t.Tier[0]+t.Tier.Substring(1).ToLower()}\n";
                        }
                    }
                    field.WithValue(temp);
                    fieldsX.Add(field);
                }
                catch(Exception ex){
                    Console.WriteLine(ex.ToString());
                    EmbedFieldBuilder field = new EmbedFieldBuilder();
                    field.WithName(n);
                    field.WithValue("Does not exist");
                    fieldsX.Add(field);                
                }
            }
            
            EmbedBuilder embeddedMessage = new EmbedBuilder();
            embeddedMessage.WithTitle("");
            embeddedMessage.WithThumbnailUrl($"http://ddragon.leagueoflegends.com/cdn/{RapiInfoService.PatchNum}/img/profileicon/{toprint[0].ProfileIconId}.png");
            embeddedMessage.WithColor(0xff69b4);
            embeddedMessage.WithFields(fieldsX);
            await ReplyAsync("", embed:embeddedMessage.Build());
        }
        
        [Command("winrate"), Summary("Get a last 20 Ranked games winrate for a player")]
        public async Task wr(params string[] names){
            if(names.Length > maxNamesWinrate)
                await Context.User.SendMessageAsync($"Too many names entered! Limit: {maxNamesWinrate}");
            else{
                int[] qs = {420, 440};
                double wins = 0, losses = 0;
                await Context.Channel.TriggerTypingAsync();
                Summoner tofind = await RapiInfoService.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, names[0]);
                Matchlist mlist = await RapiInfoService.RAPI.MatchV4.GetMatchlistAsync(Region.NA, tofind.AccountId, queue:qs, endIndex:20);
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
                embeddedMessage.WithThumbnailUrl($"http://ddragon.leagueoflegends.com/cdn/{RapiInfoService.PatchNum}/img/profileicon/{tofind.ProfileIconId}.png");
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
            
            foreach (var user in Context.Guild.Users){
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                bool isImportant = false;
                field.Name = "<b>" + user.Username + "<\b>";
                field.Value = "```";
                foreach (var role in user.Roles){
                    if (nImportant.Contains(role)){
                        isImportant = true;
                    }
                    if (!role.IsEveryone) field.Value += role.Name + "; ";
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

    
}
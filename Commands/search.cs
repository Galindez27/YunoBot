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

    [Group("search"), Alias("s"), Name("Search")]
    [Summary("Search for LoL related information.")]
    public class Search : ModuleBase<SocketCommandContext>{

        private RapiInfo _rapi;

        public Search(RapiInfo rpi){
            _rapi = rpi;
        }

        [Obsolete]
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
            return match.Teams[playerTeam].Win == "Win";
        }
        
        [Command("ingame"), Summary("Find the ranks for all other players in the specified player's currently active game"), Remarks("<Player Name>")]
        public async Task ingame(string target){
            await Context.Channel.TriggerTypingAsync();
            Summoner targetSumm = null;
            CurrentGameInfo runningGame = null;
            try {
                targetSumm = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException();
                runningGame = await _rapi.RAPI.SpectatorV4.GetCurrentGameInfoBySummonerAsync(Region.NA, targetSumm.Id) ?? throw new InvalidOperationException();
            }
            catch (InvalidDataException){
                await ReplyAsync($"{target} not found!");
                return;
            }
            catch (InvalidOperationException){
                await ReplyAsync($"{target} not currently in a game!");
                return;
            }

            List<EmbedFieldBuilder> fields =  new List<EmbedFieldBuilder>();
            
            for (int i = 0; i < runningGame.Participants.Length; i++) {

                EmbedFieldBuilder field = new EmbedFieldBuilder();
                field.IsInline = true;

                // Field name is "<team Emoji><Chamion>, first line of value is <searched for emoji><Summoner Name>
                field.Name = $"{(runningGame.Participants[i].TeamId == 100 ? ":small_red_triangle:" : ":small_blue_diamond:")}{((Champion)runningGame.Participants[i].ChampionId).Name()}";
                field.Value = $"{(runningGame.Participants[i].SummonerId == targetSumm.Id ? ":small_orange_diamond:" : "")}{runningGame.Participants[i].SummonerName}\n";

                //Iterate through ranks the player holds, currently max of three, and add the information to field value
                LeaguePosition[] positions = await _rapi.RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, runningGame.Participants[i].SummonerId);
                foreach (LeaguePosition pos in positions){
                    string queue = pos.QueueType == "RANKED_SOLO_5x5" ? "Solo/Duo" :
                                    pos.QueueType == "RANKED_FLEX_SR" ? "Flex" :
                                    pos.QueueType == "RANKED_FLEX_TT" ? "Treeline" : (pos.QueueType[0] + pos.QueueType.Substring(1).ToLower());
                    field.Value += $"{queue}: {pos.Tier[0] + pos.Tier.Substring(1).ToLower()} {pos.Rank}\n";
                }
                fields.Add(field); //Summoner field complete, add to field list and move to next player in game
            }

            EmbedBuilder toReply = new EmbedBuilder();
            DateTimeOffset gameStart = runningGame.GameStartTime != 0 ? DateTimeOffset.FromUnixTimeMilliseconds(runningGame.GameStartTime) : DateTimeOffset.Now; // Sometimes game start time was reporting 0, or 1969, so this gives an estimate if the value is zero
            toReply.WithColor(CommandHandlingService.embedColor);
            toReply.WithFooter("Game started");
            toReply.WithAuthor(new EmbedAuthorBuilder()
               .WithName(targetSumm.Name)
               .WithIconUrl($"http://ddragon.leagueoflegends.com/cdn/{_rapi.patchNum}/img/profileicon/{targetSumm.ProfileIconId}.png"));
            toReply.WithFields(fields);
            await ReplyAsync(embed:toReply.Build());
        }

        [Command("rank"), Summary("Search for summoner ranks by name"), Alias("player", "summoner", "r"), Remarks("<Player Name>, can do multiple names")]
        public async Task byname(params string[] names){
            // Catch violation of constraints
            if (names.Length > _rapi.maxSearchRankedNames){ 
                await ReplyAsync($"Too many names! Max of {_rapi.maxSearchRankedNames}.");
                return;
            }
            await Context.Channel.TriggerTypingAsync();

            EmbedBuilder toEmbed = new EmbedBuilder();
            List<EmbedFieldBuilder> fieldList = new List<EmbedFieldBuilder>();
            Summoner topSumm = null;

            // Iterate through names given to bot
            foreach (string target in names){
                EmbedFieldBuilder tempField = new EmbedFieldBuilder();
                tempField.Name = target;
                tempField.IsInline = true;

                Summoner summTarget = null;
                LeaguePosition[] positions = null; 
                try {
                    summTarget = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException(); // The search returns null if the summoner doesnt exist throw an error to catch it
                    topSumm = topSumm ?? summTarget; //When the first valid summoner is found, save it as topSumm 
                    positions = await _rapi.RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, summTarget.Id);
                    
                    // Add summoner level to the field and then add all ranks for different queues
                    tempField.Value = $"Level: {summTarget.SummonerLevel}\n";
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
            
            toEmbed.ThumbnailUrl = $"http://ddragon.leagueoflegends.com/cdn/{_rapi.patchNum}/img/profileicon/{(topSumm != null ? topSumm.ProfileIconId : 501)}.png"; //Using the topSumm, add a URL to the icon of the player
            toEmbed.WithColor(CommandHandlingService.embedColor);
            toEmbed.WithCurrentTimestamp();
            toEmbed.WithFields(fieldList);
            await ReplyAsync(embed:toEmbed.Build());
        }
        
        [Command("winrate"), Summary("Get a last 20 Ranked games winrate for a player"), Alias("wr"), Remarks("<Player Name>")]
        public async Task wr(params string[] names){
            if(names.Length > _rapi.maxSearchWinrateNames){
                await ReplyAsync($"Too many names entered! Limit: {_rapi.maxSearchWinrateNames}");
                return;
            }
            await Context.Channel.TriggerTypingAsync();
                
            List<EmbedFieldBuilder> content = new List<EmbedFieldBuilder>();
            Summoner topSumm = null;
            int[] qs = {420, 440};

            // Iterate through names provided 
            foreach (string target in names){
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                field.Name = target; //Define name here incase the summonre is found to not exist, will be overwritten later

                Summoner tofind = null;
                Matchlist mlist = null;
                double wins = 0, losses = 0, wr;
                try{
                    tofind = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException(); //Throw exception if summoner DNE
                    field.Name = tofind.Name;

                    mlist = await _rapi.RAPI.MatchV4.GetMatchlistAsync(Region.NA, tofind.AccountId, queue:qs, endIndex:20);
                    if (mlist.TotalGames == 0) {throw new InvalidOperationException();} //Throw exception if summoner has no ranked games on record
                    
                    topSumm = topSumm ?? tofind; // Save first valid summoner to top summ

                    foreach (MatchReference mref in mlist.Matches){
                        if (await _rapi.matchIsWin(mref.GameId, tofind.AccountId)) {wins++;} //Query rapi info service to utilize cached games or pull in new ones to calculate WR
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
                catch (NullReferenceException nre){ // This catch is required to function properly, not 100% certain where the exception is generated
                    await CommandHandlingService.Logger(new LogMessage(LogSeverity.Warning, "winrate", "Poorly Handled Exception", nre));
                    field.Value = "Error";
                    topSumm = tofind ?? tofind ?? topSumm;
                }

                content.Add(field);
            }
            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "winrate", "Building embedded message..."));
            EmbedBuilder embeddedMessage = new EmbedBuilder();
            embeddedMessage.WithThumbnailUrl($"http://ddragon.leagueoflegends.com/cdn/{_rapi.patchNum}/img/profileicon/{(topSumm != null ? topSumm.ProfileIconId : 501)}.png");
            embeddedMessage.WithTitle("Last Twenty Flex/SoloDuo Games");
            embeddedMessage.WithColor(CommandHandlingService.embedColor);
            embeddedMessage.WithFields(content);
            embeddedMessage.WithCurrentTimestamp();
            embeddedMessage.WithFooter(new EmbedFooterBuilder().WithText("As of"));
            await ReplyAsync("", embed:embeddedMessage.Build());
        }
    }

}
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
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
using YunoBot.Utils;

namespace YunoBot.Commands{

    [Group("search"), Alias("s"), Name("Search")]
    [Summary("Search for LoL related information.")]
    public class Search : ModuleBase<SocketCommandContext>{
        // Local Variables --------------------------
        private RapiInfo _rapi;
        private Regex nameParser;
        private string champArtUrlBase = "http://ddragon.leagueoflegends.com/cdn/img/champion/tiles/{0}_0.jpg"; // 0 - Champion name
        private string summonerIconUrlBase = "http://ddragon.leagueoflegends.com/cdn/{0}/img/profileicon/{1}.png"; // 0 - Patch number, 1 - Icon ID

        public Search(RapiInfo rpi){
            _rapi = rpi;
            nameParser = new Regex("^[0-9\\p{L} _\\.]+$");
        }

        // Helper Functions -----------------
        private string parsePositions(LeaguePosition[] positions){
            string Value = "";
            if (positions.Length == 0){ Value = "Unranked";}
            else {
                foreach (LeaguePosition pos in positions){
                    string queue = pos.QueueType == "RANKED_SOLO_5x5" ? "Solo/Duo" :
                                    pos.QueueType == "RANKED_FLEX_SR" ? "Flex" :
                                    pos.QueueType == "RANKED_FLEX_TT" ? "Treeline" : (pos.QueueType[0] + pos.QueueType.Substring(1).ToLower());
                    Value += $"{queue}: {pos.Tier[0] + pos.Tier.Substring(1).ToLower()} {pos.Rank}\n";
                }
            }
            return Value;
        }

        private string championPicUrl(Champion champ){
                    switch (champ){
                        case Champion.KAI_SA:
                            return string.Format(champArtUrlBase, "Kaisa");
                        case Champion.NUNU_WILLUMP:
                            return string.Format(champArtUrlBase, "Nunu");
                        default:
                            return string.Format(champArtUrlBase, champ.Name());                                
                    }
        }
        
        private EmbedFieldBuilder ingameFieldBuilder(CurrentGameParticipant par){
            EmbedFieldBuilder toReturn = new EmbedFieldBuilder().WithIsInline(true);
            string champ = ((Champion)par.ChampionId).ToString();
            toReturn.Name = $"{(par.TeamId == 100 ? ":small_red_triangle:":":small_blue_diamond:")}" + champ[0] + champ.Substring(1).ToLowerInvariant();
            toReturn.Value = par.SummonerName + "\n";
            return toReturn;
        }

        // -----------------------------------

        [Command("ingame"), Summary("Find the ranks for all other players in the specified player's currently active game"), Remarks("<Player Name>")]
        public async Task ingame(string target){
            await Context.Channel.TriggerTypingAsync();
            if (!nameParser.IsMatch(target)){
                await ReplyAsync($"{target} not found!");
                return;
            }
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

            string queueType;
            _rapi.RankedQueueIdToName.TryGetValue((int)runningGame.GameQueueConfigId, out queueType);
            List<Task<LeaguePosition[]>> lookupTable = new List<Task<LeaguePosition[]>>();
            Dictionary<string, EmbedFieldBuilder> fieldsTable = new Dictionary<string, EmbedFieldBuilder>();
            foreach (var player in runningGame.Participants){
                lookupTable.Add(_rapi.RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, player.SummonerId));
                fieldsTable.Add(player.SummonerId, ingameFieldBuilder(player));
            }

            while (lookupTable.Count != 0){
                var completedLookup = await Task.WhenAny(lookupTable);
                try {
                    fieldsTable[(completedLookup.Result[0].SummonerId)].Value += parsePositions(completedLookup.Result);
                    string flagLine = "None";
                    foreach(var pos in completedLookup.Result){
                        if (pos.QueueType == queueType){
                            flagLine = FlagOrgan.getRankedEmojis(FlagOrgan.getRankedFlags(pos));
                            break;
                        }
                    }
                    fieldsTable[(completedLookup.Result[0].SummonerId)].Value += "*flags*\n" + flagLine;
                    lookupTable.Remove(completedLookup);
                }
                catch (NullReferenceException){ // Summoner has no ranked positions on file, just skip any further computation
                    lookupTable.Remove(completedLookup);
                }
            }

            
            
            string queueName;
            if (!_rapi.RankedQueueIdToName.TryGetValue((int)runningGame.GameQueueConfigId, out queueName))
                queueName = "Unranked";

            EmbedBuilder toReply = new EmbedBuilder();
            DateTimeOffset gameStart = runningGame.GameStartTime != 0 ? DateTimeOffset.FromUnixTimeMilliseconds(runningGame.GameStartTime) : DateTimeOffset.Now; // Sometimes game start time was reporting 0, or 1969, so this gives an estimate if the value is zero
            toReply.WithColor(CommandHandlingService.embedColor);
            toReply.WithFooter($"{queueName} - Game Start");
            toReply.WithTimestamp(gameStart);
            toReply.WithAuthor(new EmbedAuthorBuilder()
               .WithName(targetSumm.Name)
               .WithIconUrl(string.Format(summonerIconUrlBase, _rapi.patchNum, targetSumm.ProfileIconId)));
            toReply.WithFields(fieldsTable.Values);
            await ReplyAsync("Emoji flag legend: " + FlagOrgan.RankedEmojiLegend ,embed:toReply.Build());
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

                if (nameParser.IsMatch(target)){
                    Summoner summTarget = null;
                    LeaguePosition[] positions = null;

                    try {
                        summTarget = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException(); // The search returns null if the summoner doesnt exist throw an error to catch it
                        topSumm = topSumm ?? summTarget; //When the first valid summoner is found, save it as topSumm 
                        positions = await _rapi.RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, summTarget.Id);
                        
                        // Add summoner level to the field and then add all ranks for different queues
                        tempField.Value = $"Level: {summTarget.SummonerLevel}\n";
                        foreach (LeaguePosition pos in positions){
                            string queue = pos.QueueType == Queue.RANKED_SOLO_5x5 ? "Solo/Duo" :
                                            pos.QueueType == Queue.RANKED_FLEX_SR ? "Flex" :
                                            pos.QueueType == Queue.RANKED_FLEX_TT ? "Treeline" : (pos.QueueType[0] + pos.QueueType.Substring(1).ToLower());
                            tempField.Value += $"{queue}: {pos.Tier[0] + pos.Tier.Substring(1).ToLower()} {pos.Rank}\n";
                        }
                    }
                    catch (InvalidDataException){
                        tempField.Value = "Does not exist";
                    }
                    fieldList.Add(tempField);
                }
                else {
                    tempField.Value = "Invalid name";
                    fieldList.Add(tempField);
                }
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

            Summoner topSumm = null; //Holds the first valid summoner from the names param.
            int[] qs = {420, 440};

            // Iterate through names provided 
            foreach (string target in names){
                await Context.Channel.TriggerTypingAsync();

                if (nameParser.IsMatch(target)){
                    List<EmbedFieldBuilder> content = new List<EmbedFieldBuilder>();
                    EmbedFieldBuilder wrfield = new EmbedFieldBuilder();
                    EmbedFieldBuilder statsField = new EmbedFieldBuilder();
                    Dictionary<Champion, byte> playedChamps = new Dictionary<Champion, byte>();
                    Dictionary<string, byte> playedRole = new Dictionary<string, byte>();
                    Dictionary<string, byte> playedLanes = new Dictionary<string, byte>();
                    
                    KeyValuePair<Champion, byte> topChamp = new KeyValuePair<Champion, byte>(Champion.AHRI, 0);
                    KeyValuePair<string, byte> topRole = new KeyValuePair<string, byte>("NONE", 0);
                    KeyValuePair<string, byte> topLane = new KeyValuePair<string, byte>("NONE", 0);

                    wrfield.Name = "Stats";
                    statsField.Name = "Trends";

                    Summoner tofind = null;

                    Matchlist mlist = null;
                    double wins = 0, losses = 0, wr;
                    try{
                        tofind = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException(); //Throw exception if summoner DNE

                        mlist = await _rapi.RAPI.MatchV4.GetMatchlistAsync(Region.NA, tofind.AccountId, queue:qs, endIndex:20);
                        if (mlist.TotalGames == 0) {throw new InvalidOperationException();} //Throw exception if summoner has no ranked gaymes on record
                        
                        topSumm = topSumm ?? tofind; // Save first valid summoner to top summ

                        Task<bool>[] wrCalTasks = new Task<bool>[mlist.Matches.Length];
                        for (int i = 0; i < mlist.Matches.Length; i++){
                            wrCalTasks[i] = _rapi.matchIsWin(mlist.Matches[i].GameId, tofind.AccountId);
                        }

                        foreach (MatchReference gameRef in mlist.Matches){
                            byte number;
                            if (playedChamps.TryGetValue((Champion)gameRef.Champion, out number)){
                                playedChamps[(Champion)gameRef.Champion]++;
                                if (number > topChamp.Value){ topChamp = new KeyValuePair<Champion, byte>((Champion)gameRef.Champion, playedChamps[(Champion)gameRef.Champion]);}
                            }
                            else { playedChamps.Add((Champion)gameRef.Champion, 1);}

                            if (playedLanes.TryGetValue(gameRef.Lane, out number)){
                                playedLanes[gameRef.Lane]++;
                                if (number > topLane.Value){ topLane = new KeyValuePair<string, byte>(gameRef.Lane, playedLanes[gameRef.Lane]);}
                            }
                            else { playedLanes.Add(gameRef.Lane, 1); }

                            if (playedRole.TryGetValue(gameRef.Role, out number)){
                                playedRole[gameRef.Role]++;
                                if (number > topRole.Value){ topRole = new KeyValuePair<string, byte>(gameRef.Role, playedRole[gameRef.Role]);}
                            }
                            else { playedRole.Add(gameRef.Role, 1); }
                        }
                        
                        Task.WaitAll(wrCalTasks);
                        foreach (var winTask in wrCalTasks){
                            if (winTask.Result){ wins++; }
                            else { losses++; }
                        }
                        wr = wins / (wins + losses);
                        wrfield.Value = string.Format("WR: {0:P}\nWins: {1}\nLosses: {2}", wr, wins, losses);
                        statsField.Value = string.Format("Fave Champ: {0}\nFave Lane: {1}\nFave Role: {2}", topChamp.Key.Name(), topLane.Key, topRole.Key);
                    }
                    catch (InvalidDataException){
                        wrfield.Value = "Does not exist";
                        statsField.Value = "-";
                    }
                    catch (InvalidOperationException){
                        wrfield.Value = "No ranked games on record";
                        statsField.Value = "-";
                    }
                    catch (NullReferenceException nre){ // This catch is required to function properly, not 100% certain where the exception is generated
                        await CommandHandlingService.Logger(new LogMessage(LogSeverity.Warning, "winrate", "Poorly Handled Exception", nre));
                        wrfield.Value = "Error";
                        statsField.Value = "-";
                        topSumm = tofind ?? tofind ?? topSumm;
                    }
                    content.Add(wrfield);
                    content.Add(statsField);

                    
                    await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "winrate", "Building embedded message..."));
                    EmbedBuilder embeddedMessage = new EmbedBuilder();
                    embeddedMessage.WithThumbnailUrl(championPicUrl(topChamp.Key));
                    embeddedMessage.WithTitle("Last Twenty Flex/SoloDuo Games");
                    embeddedMessage.WithAuthor(
                        new EmbedAuthorBuilder().WithIconUrl(string.Format(summonerIconUrlBase, _rapi.patchNum, (topSumm != null ? topSumm.ProfileIconId : 501)))
                        .WithName(tofind == null ? tofind.Name : target)
                    );
                    embeddedMessage.WithColor(CommandHandlingService.embedColor);
                    embeddedMessage.WithFields(content);
                    embeddedMessage.WithCurrentTimestamp();
                    embeddedMessage.WithFooter(new EmbedFooterBuilder().WithText("As of"));
                    await Context.Channel.SendMessageAsync("", embed:embeddedMessage.Build());
                }
            }
        }
    }

}
/*
    Search module declaration. All riot API search commands will be attached TO this class. 
 */


using Discord.Commands;
using System.Text.RegularExpressions;


using YunoBot.Services;

namespace YunoBot.Commands{

    [Group("search"), Alias("s"), Name("Search")]
    [Summary("Search for LoL related information.")]
    public partial class Search : ModuleBase<SocketCommandContext>{
        // Local Variables --------------------------
        private RapiInfo _rapi;
        private Regex nameParser;
        private string champArtUrlBase = "http://ddragon.leagueoflegends.com/cdn/img/champion/tiles/{0}_0.jpg"; // 0 - Champion name
        private string summonerIconUrlBase = "http://ddragon.leagueoflegends.com/cdn/{0}/img/profileicon/{1}.png"; // 0 - Patch number, 1 - Icon ID

        public Search(RapiInfo rpi){
            _rapi = rpi;
            nameParser = new Regex("^[0-9\\p{L} _\\.]+$");
        }
    }
}
        


//         // -----------------------------------

//         [Command("ingame"), Summary("Find the ranks for all other players in the specified player's currently active game"), Remarks("<Player Name>")]
//         public async Task ingame(string target){
//             await Context.Channel.TriggerTypingAsync();
//             if (!nameParser.IsMatch(target)){
//                 await ReplyAsync($"{target} not found!");
//                 return;
//             }
//             Summoner targetSumm = null;
//             CurrentGameInfo runningGame = null;
//             try {
//                 targetSumm = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException();
//                 runningGame = await _rapi.RAPI.SpectatorV4.GetCurrentGameInfoBySummonerAsync(Region.NA, targetSumm.Id) ?? throw new InvalidOperationException();
//             }
//             catch (InvalidDataException){
//                 await ReplyAsync($"{target} not found!");
//                 return;
//             }
//             catch (InvalidOperationException){
//                 await ReplyAsync($"{target} not currently in a game!");
//                 return;
//             }

//             string queueType;
//             _rapi.RankedQueueIdToName.TryGetValue((int)runningGame.GameQueueConfigId, out queueType);
//             List<Task<LeaguePosition[]>> lookupTable = new List<Task<LeaguePosition[]>>();
//             Dictionary<string, EmbedFieldBuilder> fieldsTable = new Dictionary<string, EmbedFieldBuilder>();
//             foreach (var player in runningGame.Participants){
//                 lookupTable.Add(_rapi.RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, player.SummonerId));
//                 fieldsTable.Add(player.SummonerId, ingameFieldBuilder(player));
//             }

//             while (lookupTable.Count != 0){
//                 var completedLookup = await Task.WhenAny(lookupTable);
//                 try {
//                     fieldsTable[(completedLookup.Result[0].SummonerId)].Value += parsePositions(completedLookup.Result);
//                     string flagLine = "None";
//                     foreach(var pos in completedLookup.Result){
//                         if (pos.QueueType == queueType){
//                             flagLine = FlagOrgan.getRankedEmojis(FlagOrgan.getRankedFlags(pos));
//                             break;
//                         }
//                     }
//                     fieldsTable[(completedLookup.Result[0].SummonerId)].Value += "*flags*\n" + flagLine;
//                     lookupTable.Remove(completedLookup);
//                 }
//                 catch (NullReferenceException){ // Summoner has no ranked positions on file, just skip any further computation
//                     lookupTable.Remove(completedLookup);
//                 }
//             }

            
            
//             string queueName;
//             if (!_rapi.RankedQueueIdToName.TryGetValue((int)runningGame.GameQueueConfigId, out queueName))
//                 queueName = "Unranked";

//             EmbedBuilder toReply = new EmbedBuilder();
//             DateTimeOffset gameStart = runningGame.GameStartTime != 0 ? DateTimeOffset.FromUnixTimeMilliseconds(runningGame.GameStartTime) : DateTimeOffset.Now; // Sometimes game start time was reporting 0, or 1969, so this gives an estimate if the value is zero
//             toReply.WithColor(CommandHandlingService.embedColor);
//             toReply.WithFooter($"{queueName} - Game Start");
//             toReply.WithTimestamp(gameStart);
//             toReply.WithAuthor(new EmbedAuthorBuilder()
//                .WithName(targetSumm.Name)
//                .WithIconUrl(string.Format(summonerIconUrlBase, _rapi.patchNum, targetSumm.ProfileIconId)));
//             toReply.WithFields(fieldsTable.Values);
//             await ReplyAsync("Emoji flag legend: " + FlagOrgan.RankedEmojiLegend ,embed:toReply.Build());
//         }

        
//         [Command("winrate"), Summary("Get a last 20 Ranked games winrate for a player"), Alias("wr"), Remarks("<Player Name>")]
//         public async Task wr(params string[] names){
//             if(names.Length > _rapi.maxSearchWinrateNames){
//                 await ReplyAsync($"Too many names entered! Limit: {_rapi.maxSearchWinrateNames}");
//                 return;
//             }

//             Summoner topSumm = null; //Holds the first valid summoner from the names param.
//             int[] qs = {420, 440};

//             // Iterate through names provided 
//             foreach (string target in names){
//                 await Context.Channel.TriggerTypingAsync();

//                 if (nameParser.IsMatch(target)){
//                     List<EmbedFieldBuilder> content = new List<EmbedFieldBuilder>();
//                     EmbedFieldBuilder wrfield = new EmbedFieldBuilder();
//                     EmbedFieldBuilder statsField = new EmbedFieldBuilder();
//                     Dictionary<Champion, byte> playedChamps = new Dictionary<Champion, byte>();
//                     Dictionary<string, byte> playedRole = new Dictionary<string, byte>();
//                     Dictionary<string, byte> playedLanes = new Dictionary<string, byte>();
                    
//                     KeyValuePair<Champion, byte> topChamp = new KeyValuePair<Champion, byte>(Champion.AHRI, 0);
//                     KeyValuePair<string, byte> topRole = new KeyValuePair<string, byte>("NONE", 0);
//                     KeyValuePair<string, byte> topLane = new KeyValuePair<string, byte>("NONE", 0);

//                     wrfield.Name = "Stats";
//                     statsField.Name = "Trends";

//                     Summoner tofind = null;

//                     Matchlist mlist = null;
//                     double wins = 0, losses = 0, wr;
//                     try{
//                         tofind = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException(); //Throw exception if summoner DNE

//                         mlist = await _rapi.RAPI.MatchV4.GetMatchlistAsync(Region.NA, tofind.AccountId, queue:qs, endIndex:20);
//                         if (mlist.TotalGames == 0) {throw new InvalidOperationException();} //Throw exception if summoner has no ranked gaymes on record
                        
//                         topSumm = topSumm ?? tofind; // Save first valid summoner to top summ

//                         Task<bool>[] wrCalTasks = new Task<bool>[mlist.Matches.Length];
//                         for (int i = 0; i < mlist.Matches.Length; i++){
//                             wrCalTasks[i] = _rapi.matchIsWin(mlist.Matches[i].GameId, tofind.AccountId);
//                         }

//                         foreach (MatchReference gameRef in mlist.Matches){
//                             byte number;
//                             if (playedChamps.TryGetValue((Champion)gameRef.Champion, out number)){
//                                 playedChamps[(Champion)gameRef.Champion]++;
//                                 if (number > topChamp.Value){ topChamp = new KeyValuePair<Champion, byte>((Champion)gameRef.Champion, playedChamps[(Champion)gameRef.Champion]);}
//                             }
//                             else { playedChamps.Add((Champion)gameRef.Champion, 1);}

//                             if (playedLanes.TryGetValue(gameRef.Lane, out number)){
//                                 playedLanes[gameRef.Lane]++;
//                                 if (number > topLane.Value){ topLane = new KeyValuePair<string, byte>(gameRef.Lane, playedLanes[gameRef.Lane]);}
//                             }
//                             else { playedLanes.Add(gameRef.Lane, 1); }

//                             if (playedRole.TryGetValue(gameRef.Role, out number)){
//                                 playedRole[gameRef.Role]++;
//                                 if (number > topRole.Value){ topRole = new KeyValuePair<string, byte>(gameRef.Role, playedRole[gameRef.Role]);}
//                             }
//                             else { playedRole.Add(gameRef.Role, 1); }
//                         }
                        
//                         Task.WaitAll(wrCalTasks);
//                         foreach (var winTask in wrCalTasks){
//                             if (winTask.Result){ wins++; }
//                             else { losses++; }
//                         }
//                         wr = wins / (wins + losses);
//                         wrfield.Value = string.Format("WR: {0:P}\nWins: {1}\nLosses: {2}", wr, wins, losses);
//                         statsField.Value = string.Format("Fave Champ: {0}\nFave Lane: {1}\nFave Role: {2}", topChamp.Key.Name(), topLane.Key, topRole.Key);
//                     }
//                     catch (InvalidDataException){
//                         wrfield.Value = "Does not exist";
//                         statsField.Value = "-";
//                     }
//                     catch (InvalidOperationException){
//                         wrfield.Value = "No ranked games on record";
//                         statsField.Value = "-";
//                     }
//                     catch (NullReferenceException nre){ // This catch is required to function properly, not 100% certain where the exception is generated
//                         await CommandHandlingService.Logger(new LogMessage(LogSeverity.Warning, "winrate", "Poorly Handled Exception", nre));
//                         wrfield.Value = "Error";
//                         statsField.Value = "-";
//                         topSumm = tofind ?? tofind ?? topSumm;
//                     }
//                     content.Add(wrfield);
//                     content.Add(statsField);

                    
//                     await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "winrate", "Building embedded message..."));
//                     EmbedBuilder embeddedMessage = new EmbedBuilder();
//                     embeddedMessage.WithThumbnailUrl(championPicUrl(topChamp.Key));
//                     embeddedMessage.WithTitle("Last Twenty Flex/SoloDuo Games");
//                     embeddedMessage.WithAuthor(
//                         new EmbedAuthorBuilder().WithIconUrl(string.Format(summonerIconUrlBase, _rapi.patchNum, (topSumm != null ? topSumm.ProfileIconId : 501)))
//                         .WithName(tofind == null ? tofind.Name : target)
//                     );
//                     embeddedMessage.WithColor(CommandHandlingService.embedColor);
//                     embeddedMessage.WithFields(content);
//                     embeddedMessage.WithCurrentTimestamp();
//                     embeddedMessage.WithFooter(new EmbedFooterBuilder().WithText("As of"));
//                     await Context.Channel.SendMessageAsync("", embed:embeddedMessage.Build());
//                 }
//             }
//         }
//     }

// }
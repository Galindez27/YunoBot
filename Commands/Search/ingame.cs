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

namespace YunoBot.Commands{
    public partial class Search{
        
        [Command("ingame"), Summary("Find the ranks for all other players in the specified player's currently active game"), Remarks("<Player Name>")]
        public async Task ingame(string target){
            await Context.Channel.TriggerTypingAsync();
            if (!nameParser.IsMatch(target)){
                await ReplyAsync($"Invalid name: {target}");
                return;
            }

            Summoner targetSumm = null;
            CurrentGameInfo runningGame = null;
            string queueType;

            // Assure that a valid name is an actual summoner name and is in a game
            try {
                targetSumm = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException();
                runningGame = await _rapi.RAPI.SpectatorV4.GetCurrentGameInfoBySummonerAsync(Region.NA, targetSumm.Id) ?? throw new InvalidOperationException();
            }
            catch (InvalidDataException){
                await ReplyAsync($"{target} not found!");
                return;
            }
            catch (InvalidOperationException){
                await ReplyAsync($"{target} is not currently in a game!");
                return;
            }

            //Find the queue type, create a dictionary to be filled with players needing to be looked up
            _rapi.RankedQueueIdToName.TryGetValue((int)runningGame.GameQueueConfigId, out queueType);
            Dictionary<string, EmbedFieldBuilder> fieldsTable = new Dictionary<string, EmbedFieldBuilder>();
            Task<LeagueEntry[]>[] lookupList = new Task<LeagueEntry[]>[runningGame.Participants.Length];
            for (int i =0; i < runningGame.Participants.Length; i++){
                lookupList[i] = (_rapi.RAPI.LeagueV4.GetLeagueEntriesForSummonerAsync(_rapi.CurrRegion, runningGame.Participants[i].SummonerId));
                fieldsTable.Add(runningGame.Participants[i].SummonerId, ingameFieldBuilder(runningGame.Participants[i]));
                fieldsTable[runningGame.Participants[i].SummonerId].Value = runningGame.Participants[i].SummonerName + '\n'; //Adds the player name as the first field value
            }

            //Await all player ranks to be found then add them to the fields.
            Task.WaitAll(lookupList);
            foreach (var playerRank in lookupList){
                if(playerRank.Result.Length != 0){
                    fieldsTable[playerRank.Result[0].SummonerId].Value += parsePositions(playerRank.Result);
                }
            }

            //Resolve string name to be unranked if playing an unranked queue
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
            await ReplyAsync(embed : toReply.Build());
        }

    }
}
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

//         
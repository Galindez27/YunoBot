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

//         private EmbedFieldBuilder ingameFieldBuilder(CurrentGameParticipant par){
//             EmbedFieldBuilder toReturn = new EmbedFieldBuilder().WithIsInline(true);
//             string champ = ((Champion)par.ChampionId).ToString();
//             toReturn.Name = $"{(par.TeamId == 100 ? ":small_red_triangle:":":small_blue_diamond:")}" + champ[0] + champ.Substring(1).ToLowerInvariant();
//             toReturn.Value = par.SummonerName + "\n";
//             return toReturn;
//         }
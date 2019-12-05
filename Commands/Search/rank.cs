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
    partial class Search{
        [Command("rank"), Summary("Search for summoner ranks by name"), Alias("player", "summoner", "r"), Remarks("<Player Name>, can do multiple names")]
        public async Task byname(params string[] names){
            // Catch violation of constraints
            if (names.Length > _rapi.maxSearchRankedNames){ 
                await ReplyAsync($"Too many names! Max of {_rapi.maxSearchRankedNames}.");
                return;
            }

            // No names provided, try using discord usernname
            else if (names.Length == 0){
                if (!nameParser.IsMatch(Context.User.Username)){
                await ReplyAsync($"{Context.User.Username} is not a valid League username!");
                return;
            }
            Summoner summ;
            LeagueEntry[] ranks;
            try{
                summ = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(_rapi.CurrRegion, Context.User.Username) ?? throw new InvalidDataException();
                ranks = await _rapi.RAPI.LeagueV4.GetLeagueEntriesForSummonerAsync(_rapi.CurrRegion, summ.Id) ?? throw new InvalidDataException();
            }
            catch(InvalidDataException){
                await ReplyAsync($"{Context.User.Username} does not have information available!");
                return;
            }
            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "Search Rank", $"Context username: {Context.User.Username}"));
            await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "Search Rank", $"Summoner Name: {summ.Name}"));
            var toEmbed = new EmbedBuilder().WithAuthor(new EmbedAuthorBuilder().WithName(Context.User.Username));
            var tempField = new EmbedFieldBuilder();
            tempField.Name = "-";
            tempField.IsInline = true;
            tempField.Value = parsePositions(ranks);
            toEmbed.AddField(tempField);
            toEmbed.ThumbnailUrl = $"http://ddragon.leagueoflegends.com/cdn/{_rapi.patchNum}/img/profileicon/{(summ.ProfileIconId)}.png";
            toEmbed.WithColor(CommandHandlingService.embedColor);
            toEmbed.WithCurrentTimestamp();
            await ReplyAsync(embed:toEmbed.Build());
            }

            // Search names provided
            else {
                await Context.Channel.TriggerTypingAsync();

                EmbedBuilder toEmbed = new EmbedBuilder().WithAuthor(new EmbedAuthorBuilder().WithName(names.Length == 1 ? names[0] : "Rank Search Results"));
                List<EmbedFieldBuilder> fieldList = new List<EmbedFieldBuilder>();
                Summoner topSumm = null;

                // Iterate through names given to bot
                foreach (string target in names){
                    EmbedFieldBuilder tempField = new EmbedFieldBuilder();
                    tempField.Name = names.Length == 1 ? "-" : target;
                    tempField.IsInline = true;

                    if (nameParser.IsMatch(target)){
                        Summoner summTarget = null;
                        LeagueEntry[] positions = null;

                        try {
                            summTarget = await _rapi.RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, target) ?? throw new InvalidDataException(); // The search returns null if the summoner doesnt exist throw an error to catch it
                            topSumm = topSumm ?? summTarget; //When the first valid summoner is found, save it as topSumm 
                            positions = await _rapi.RAPI.LeagueV4.GetLeagueEntriesForSummonerAsync(Region.NA, summTarget.Id);
                            
                            // Add summoner level to the field and then add all ranks for different queues
                            tempField.Value = $"Level: {summTarget.SummonerLevel}\n" + parsePositions(positions);
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
        }


    }
}
using MingweiSamuel.Camille.LeagueV4;
using MingweiSamuel.Camille.Enums;
using System.Collections.Generic;

namespace YunoBot.Commands{
    public partial class Search{
        private string parsePositions(LeagueEntry[] entries){
            var ranks = (soloduo: "Unranked", flex5v5: "Unranked", flex3v3:"", unknown: "");
            foreach(LeagueEntry entry in entries){
                string thisEntryRank = entry.Tier[0] + entry.Tier.Substring(1).ToLower() + " " + entry.Rank;
                switch (entry.QueueType){
                    case Queue.RANKED_SOLO_5x5:
                        ranks.soloduo = thisEntryRank;
                        break;
                    case Queue.RANKED_FLEX_SR:
                        ranks.flex5v5 = thisEntryRank;
                        break;
                    case Queue.RANKED_FLEX_TT:
                        ranks.flex3v3 = thisEntryRank;
                        break;
                    default:
                        ranks.unknown = thisEntryRank;
                        break;
                }
            }
            return $"Solo/Duo: {ranks.soloduo}\nFlex: {ranks.flex5v5}{(ranks.flex3v3 ?? "/nFlex 3v3: " + ranks.flex3v3)}";
        }
    }
}
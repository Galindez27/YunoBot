// using MingweiSamuel.Camille.LeagueV4;
// using System;
// using System.Collections.Concurrent;
// using Discord;

// namespace YunoBot.Utils{
//     [Flags]
//     public enum RankedFlag{
//         hotStreak = 0x1,
//         veteran = 0x2,
//         inactive = 0x4,
//         noob = 0x8,
//         miniSeries = 0x10,
//         WRlessThanFifty = 0x20,
//         WRmoreThanSixty = 0x40,
//         perfect = 0x80,
//         smurf = 0x100,
//     }

//     public static class FlagOrgan{
//         public static readonly ConcurrentDictionary<RankedFlag, String> RankedEmojiMap;
//         public static readonly string RankedEmojiLegend;
        
//         static FlagOrgan(){
//             RankedEmojiMap = new ConcurrentDictionary<RankedFlag, String>();
//             RankedEmojiMap.TryAdd(RankedFlag.hotStreak, "🔥");
//             RankedEmojiMap.TryAdd(RankedFlag.veteran, "⚜");
//             RankedEmojiMap.TryAdd(RankedFlag.inactive, "💤");
//             RankedEmojiMap.TryAdd(RankedFlag.noob, "🔰");
//             RankedEmojiMap.TryAdd(RankedFlag.miniSeries, "🔜");
//             RankedEmojiMap.TryAdd(RankedFlag.WRlessThanFifty, "⚠");
//             RankedEmojiMap.TryAdd(RankedFlag.WRmoreThanSixty, "📈");
//             RankedEmojiMap.TryAdd(RankedFlag.perfect, "💯");
//             RankedEmojiMap.TryAdd(RankedFlag.smurf, "⁉");
//             RankedEmojiLegend = "";
//             foreach (RankedFlag k in RankedEmojiMap.Keys){
//                 RankedEmojiLegend += $"{RankedEmojiMap[k]}: {k}, ";
//             }
//         }

//         public static RankedFlag getRankedFlags(LeaguePosition position){
//             RankedFlag builder = 0;
//             builder |= (position.HotStreak ? RankedFlag.hotStreak : 0);
//             builder |= (position.Veteran ? RankedFlag.veteran : 0);
//             builder |= (position.Inactive ? RankedFlag.inactive : 0);
//             builder |= (position.FreshBlood ? RankedFlag.noob : 0);
//             builder |= ((position.MiniSeries != null) ? RankedFlag.miniSeries : 0);
//             builder |= (((double)position.Wins/(position.Wins + position.Losses) < 0.5) ? RankedFlag.WRlessThanFifty : 0);
//             builder |= (((double)position.Wins/(position.Wins + position.Losses) > 0.6) ? RankedFlag.WRmoreThanSixty : 0);
//             builder |= (((double)position.Wins/(position.Wins + position.Losses) == 1) ? RankedFlag.perfect : 0);
//             builder |= (((double)position.Wins/(position.Wins + position.Losses) > 0.6 && (position.Wins + position.Losses > 20)) ? RankedFlag.smurf : 0);
//             return builder;
//         }

//         public static string getRankedEmojis(RankedFlag flagmask){
//             string toRet = "";
//             foreach (var f in RankedEmojiMap.Keys){
//                 if (flagmask.HasFlag(f)){
//                     toRet += RankedEmojiMap[f];
//                 }
//             }
//             return toRet;
//         }
//     }
// }
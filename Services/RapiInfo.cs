using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;

using Microsoft.Extensions.DependencyInjection;

using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.LeagueV4;
using MingweiSamuel.Camille.MatchV4;

using YunoBot.Commands;

namespace YunoBot.Services{
    public class RapiInfo{
        public RiotApi RAPI;
        private string PatchNum = "00";
        private string cacheFileName;
    
        private int _MaxWinrateNames = 1;
        private int _MaxRankedNames = 5;
        private ConcurrentDictionary<long, StoredMatch> gameCache;

        public string patchNum {get { return PatchNum;}}
        public int maxSearchRankedNames { get { return _MaxRankedNames;}}
        public int maxSearchWinrateNames { get { return _MaxWinrateNames;}}

        public readonly ConcurrentDictionary<string, int> RankedQueueNameToId;
        public readonly ConcurrentDictionary<int, string> RankedQueueIdToName;

        public readonly Region CurrRegion = Region.NA;

        
        public RapiInfo(String key){
            RAPI = RiotApi.NewInstance(key);
            updateLeaguePatch().GetAwaiter().GetResult();

            RankedQueueNameToId = new ConcurrentDictionary<string, int>();
            RankedQueueIdToName = new ConcurrentDictionary<int, string>();
            RankedQueueNameToId.TryAdd(Queue.RANKED_FLEX_SR, 440);
            RankedQueueNameToId.TryAdd(Queue.RANKED_SOLO_5x5, 420);
            RankedQueueNameToId.TryAdd(Queue.RANKED_FLEX_TT, 470);
            foreach (var x in RankedQueueNameToId.Keys){
                RankedQueueIdToName.TryAdd(RankedQueueNameToId[x], x);
            }
        }

        public async Task updateLeaguePatch(){
            WebRequest temp = WebRequest.Create("https://ddragon.leagueoflegends.com/api/versions.json");
            WebResponse k = await temp.GetResponseAsync();
            Stream resp = k.GetResponseStream();
            StreamReader reader = new StreamReader(resp, System.Text.Encoding.GetEncoding("utf-8"));
            String jsonString = await reader.ReadToEndAsync();
            var json = JsonConvert.DeserializeObject<List<String>>(jsonString);
            lock(PatchNum){
                PatchNum = json[0];
            }
        }
    
        private void pullInGames(){
            using (Stream s = File.OpenRead(cacheFileName)){
                BinaryFormatter formatter = new BinaryFormatter();
                ConcurrentDictionary<long, StoredMatch> temp = new ConcurrentDictionary<long, StoredMatch>(formatter.Deserialize(s) as Dictionary<long, StoredMatch>);
                gameCache = temp;
            }
        }
        private void startNewCache(){
            gameCache = new ConcurrentDictionary<long, StoredMatch>();
        }

        public void dumpCache(){
            CommandHandlingService.Logger(new LogMessage(LogSeverity.Info, "RapiInfo", $"Dumping Cache to: {cacheFileName}"));
            using (Stream s = File.Create(cacheFileName)){
                BinaryFormatter formatter = new BinaryFormatter();
                Dictionary<long, StoredMatch> temp = null;
                lock (gameCache){
                    temp = new Dictionary<long, StoredMatch>(gameCache);
                }
                formatter.Serialize(s, temp);
            }
        }
        public void setCacheFile(string fname){
            cacheFileName = fname;
            if (File.Exists(fname)){
                CommandHandlingService.Logger(new LogMessage(LogSeverity.Verbose, "RapiInfoService", "Cache file found"));
                pullInGames();
            }
            else {
                CommandHandlingService.Logger(new LogMessage(LogSeverity.Verbose, "RapiInfoService", "No cache file found. Starting new cache"));
                startNewCache();
            }
        }
        
        public async Task<bool> matchIsWin(long id, string accId){
            StoredMatch temp = null;
            if (gameCache.TryGetValue(id, out temp)){
                await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "RAPI wincheck", $"mref:{id, 11} | Found in cache"));
                return temp.winners == temp.playerTeams[accId];
            }
            else {
                await CommandHandlingService.Logger(new LogMessage(LogSeverity.Debug, "RAPI wincheck", $"mref:{id, 11} | Not in cache, retrieving..."));
                temp = new StoredMatch(await RAPI.MatchV4.GetMatchAsync(Region.NA, id));
                gameCache.TryAdd(temp.id, temp);
                return temp.winners == temp.playerTeams[accId];
            }
        }

        ~RapiInfo(){
            dumpCache();
        }
    }
}
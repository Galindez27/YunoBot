﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System.Threading.Tasks;
using System.Collections.Generic;
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
        private Dictionary<long, StoredMatch> gameCache;

        public string patchNum {get { return PatchNum;}}
        public int maxSearchRankedNames { get { return _MaxRankedNames;}}
        public int maxSearchWinrateNames { get { return _MaxWinrateNames;}}

        
        public RapiInfo(String key, IServiceProvider services){
            RAPI = RiotApi.NewInstance(key);
            updateLeaguePatch().GetAwaiter().GetResult();
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
                Dictionary<long, StoredMatch> temp = formatter.Deserialize(s) as Dictionary<long, StoredMatch>;
                gameCache = temp;
            }
        }
        private void startNewCache(){
            gameCache = new Dictionary<long, StoredMatch>();
        }
        
        public void dumpCache(){
            CommandHandlingService.Logger(new LogMessage(LogSeverity.Info, "RapiInfo", $"Dumping Cache to: {cacheFileName}"));
            using (Stream s = File.Create(cacheFileName)){
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(s, gameCache);
            }
        }
        public void setCacheFile(string fname){
            cacheFileName = fname;
            if (File.Exists(fname)){
                pullInGames();
            }
            else {
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
                gameCache.Add(temp.id, temp);
                return temp.winners == temp.playerTeams[accId];
            }
        }

        ~RapiInfo(){
            dumpCache();
        }
    }
}
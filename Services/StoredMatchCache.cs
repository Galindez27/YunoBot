using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

using System.Threading;
using System.Timers;

using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.MatchV4;
using MingweiSamuel.Camille.Util;

namespace YunoBot.Services{
    
    public class StoredGameCache{
        public readonly int MaxTableSize;
        public readonly int MaxCacheTables;
        public readonly int MaxLoadTables;

        private SortedSet<Dictionary<long, StoredMatch>> _MainSet;
        private SortedSet<Dictionary<long, StoredMatch>> _LoadSet;
        private Dictionary<long, long> manifest;

        private RapiInfo _rapiInfoService;
        
        public StoredGameCache(RapiInfo rapiInfoServie,int tableSize, int cacheTables, int loadTables){
            
        }
        public StoredGameCache(RapiInfo rapiInfoServie,int tableSize, int cacheTables, int loadTables, Dictionary<long, long> LoadedMan){
        }
    }
}
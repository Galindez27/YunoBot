using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

using System.Threading.Tasks;
using System.Timers;

using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.MatchV4;
using MingweiSamuel.Camille.Util;

// namespace YunoBot.Services{
//     [Serializable]
//     public struct savedGameTable{
//         long newestDate;
//         Dictionary<long, StoredMatch> table;
//     }

//     public class StoredGameCache{
//         private static byte obnum = 0;

//         public readonly int MaxTableSize;
//         public readonly int MaxCacheTables;
//         public readonly int MaxLoadTables;

//         private Queue<savedGameTable> _MainQueue;
//         private Dictionary<long, savedGameTable> _LoadSet;
//         private <long> manifest;
//         private Dictionary<long, string> fileManifest;

//         private RiotApi _rpi;
        
//         public StoredGameCache(ref RiotApi api,int tableSize, int cacheTables, int loadTables){
//             MaxCacheTables = cacheTables;
//             MaxTableSize = tableSize;
//             MaxLoadTables = loadTables;
//             _rpi = api;
//             manifest = new Dictionary<long, long>();
//             obnum++;
//             if (obnum > 1) {throw new InvalidProgramException("More than one game cache object created");}
//         }
//         public StoredGameCache(ref RiotApi api,int tableSize, int cacheTables, int loadTables, Dictionary<long, long> LoadedMan, Dictionary<long, string> fileMan){
//             MaxCacheTables = cacheTables;
//             MaxTableSize = tableSize;
//             MaxLoadTables = loadTables;
//             _rpi = api;
//             manifest = LoadedMan;
//             obnum++;
//             if (obnum > 1) {throw InvalidProgramException("More than one game cache object created");}
//         }

//         public async Task<StoredMatch> findGame(long toFind){
//             StoredMatch temp = null;
//             if ()
//         }
        
//     }
// }
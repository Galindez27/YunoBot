using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.MatchV4;
using MingweiSamuel.Camille.Util;



namespace YunoBot.Services{
    [Serializable]
    public struct KDACSGOLD{
        public short kills;
        public short deaths;
        public short assists;
        public short cs;
        public ushort gold;
    };

    [Serializable]
    public class StoredMatch{
        public readonly List<string> players;
        public readonly Dictionary<string, byte> playerTeams; // AccountId to Team (0 for blue, 1 for red)
        public readonly byte winners; // Which team won
        public readonly Dictionary<string, int> playerChampions; // AccountId to Champion
        public readonly Dictionary<string, KDACSGOLD> playerSimpleStats; // AccountId to K/D/A, CS, and Gold
        public readonly byte season;
        public readonly long date;
        public readonly long id;
        
        public StoredMatch(Match toStore){
            //
            // Summary:
            //      An container class designed to pull out important information from a
            //      Camille Match data class to be stored in a smaller size.
            //

            List<string> accs = new List<string>();
            Dictionary<string, byte> tPlayers = new Dictionary<string, byte>();
            Dictionary<string, bool> tWinners = new Dictionary<string, bool>();
            Dictionary<string, int> tChampions = new Dictionary<string, int>();
            Dictionary<string, KDACSGOLD> tStats = new Dictionary<string, KDACSGOLD>();

            foreach (ParticipantIdentity parId in toStore.ParticipantIdentities){
                string workingId = parId.Player.AccountId;
                byte TeamId = (byte)(toStore.Participants[parId.ParticipantId - 1].TeamId == 100 ? 0 : 1);
                bool isWin = !(toStore.Teams[TeamId].Win[0] == 'W');
                int champ = toStore.Participants[parId.ParticipantId - 1].ChampionId;
                KDACSGOLD stats = new KDACSGOLD{ 
                    kills = (short) toStore.Participants[parId.ParticipantId - 1].Stats.Kills,
                    deaths = (short) toStore.Participants[parId.ParticipantId - 1].Stats.Deaths,
                    assists = (short) toStore.Participants[parId.ParticipantId - 1].Stats.Assists,
                    cs = (short) toStore.Participants[parId.ParticipantId - 1].Stats.TotalMinionsKilled,
                    gold = (ushort) toStore.Participants[parId.ParticipantId - 1].Stats.GoldEarned
                };

                accs.Add(workingId);
                tPlayers.Add(workingId, TeamId);
                tWinners.Add(workingId, isWin);
                tChampions.Add(workingId, champ);
                tStats.Add(workingId, stats);
            }
            players = accs;
            playerTeams = tPlayers;
            winners = toStore.Teams[0].Win[0] == 'W' ? (byte)0 : (byte)1;
            playerChampions = tChampions;
            playerSimpleStats = tStats;
            season = (byte)toStore.SeasonId;
            date = toStore.GameCreation;
            id = toStore.GameId;
        }

        public override string ToString() {
            string timeForm = "u";
            string f = "{0, -48}| {1, -4} | {2, -13} | {3,2}/{4,2}/{5,2} | {6}\n";
            string toRet = $"GameId: {id, -10} | Date: {new DateTime().AddYears(1969).AddMilliseconds(date).ToLocalTime().ToString(timeForm, CultureInfo.CreateSpecificCulture("en-US"))} Winners: {(winners == 1 ? "Red" : "Blue")}\n";
            toRet += string.Format(f, "Player AccountId", "Team", "Champion", "K", "D", "A", "Gold");
            for (int i = 0; i < 10; i++){
                string accId = players[i];
                toRet += string.Format(f,
                        accId,
                        (playerTeams[accId] == 1 ? "Red" : "Blue"),
                        ((Champion)playerChampions[accId]).Name(),
                        playerSimpleStats[accId].kills,
                        playerSimpleStats[accId].deaths,
                        playerSimpleStats[accId].assists,
                        playerSimpleStats[accId].gold
                        );
            }

            return toRet;
        }
    };

    static class StoredMatchHelper{

        public static bool checkWin(StoredMatch toPeek, string toLookFor){
            byte tempteam = 2;
            if (toPeek.playerTeams.TryGetValue(toLookFor, out tempteam)){
                return toPeek.winners == tempteam;
            } 
            else { throw new ArgumentException($"Player '{toLookFor}' not found in StoredMatch: {toPeek.id}."); }
        }  

    };


}
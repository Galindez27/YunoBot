using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System;
using System.IO;
using System.Security.Cryptography;

using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SummonerV4;
using MingweiSamuel.Camille.LeagueV4;
using MingweiSamuel.Camille.MatchV4;



public class Info : ModuleBase{
    [Command("hello"), Summary("Say hello. Simple Ping")]
    public async Task Say(string remainder){
        await ReplyAsync($"Hello! {Context.User.Username} said: {remainder}");
    }

    [Command("hello"), Summary("Say hello2")]
    public async Task Say(){
        await ReplyAsync("Hello!");
    }

    // [Command("help"), Summary("Help")]
    // public async Task help(){
        
    // }
}

[Group("search"), Summary("Search for information")]
public class Search : ModuleBase{


    private RiotApi RAPI;
    private int maxNames = 5;
    private string DDragonTailVer = "9.3.1";


    private static Task Logger(LogMessage message){
        var cc = Console.ForegroundColor;
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }
        Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,-8}] {message.Source}: {message.Message}");
        Console.ForegroundColor = cc;
        
        // If you get an error saying 'CompletedTask' doesn't exist,
        // your project is targeting .NET 4.5.2 or lower. You'll need
        // to adjust your project's target framework to 4.6 or higher
        // (instructions for this are easily Googled).
        // If you *need* to run on .NET 4.5 for compat/other reasons,
        // the alternative is to 'return Task.Delay(0);' instead.
        return Task.CompletedTask;
    }

    public Search(){
        
        string token;
        using (StreamReader tfile = File.OpenText("tokenfile")){
            while(true){
                token = tfile.ReadLine();
                if(token == "riotKey"){
                    token = tfile.ReadLine();
                    break;
                }
            }
        }
        RAPI = RiotApi.NewInstance(token);
        
        //update datadragontail version, blocked to allow conensing in editor
        {
        WebRequest temp = WebRequest.Create("https://ddragon.leagueoflegends.com/api/versions.json");
        WebResponse k = temp.GetResponseAsync().GetAwaiter().GetResult();
        Stream resp = k.GetResponseStream();
        StreamReader reader = new StreamReader(resp, System.Text.Encoding.GetEncoding("utf-8"));
        Char[] t = new Char[32];
        int count = reader.Read(t, 0, 32);
        int start = -1, stop = -1;
        for (int i = 0; i < count; i++){
            if (start == -1 && t[i] == '\"'){
                start = i+1;
            }
            else if (t[i] == '\"'){
                stop = i-2;
                break;
            }
        }
        DDragonTailVer = new String(t, start, stop);
        Logger(new LogMessage(LogSeverity.Info, "Search Group", "DDver "+DDragonTailVer));
        }

    }

    [Command("rank"), Summary("Search for summoner ranks by name")]
    public async Task byname(params string[] names){
        if (names.Length > maxNames){ 
            await ReplyAsync($"Too many names! Max of {maxNames}.");
            return;
            }
        await Context.Channel.TriggerTypingAsync();
        List<Summoner> toprint = new List<Summoner>();
        List<EmbedFieldBuilder> fieldsX = new List<EmbedFieldBuilder>();
        foreach (string n in names){
            try{
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                Summoner summ = await RAPI.SummonerV4.GetBySummonerNameAsync(Region.NA, n);
                LeaguePosition[] leagueInfo = await RAPI.LeagueV4.GetAllLeaguePositionsForSummonerAsync(Region.NA, summ.Id);

                toprint.Add(summ);
                field.WithName(summ.Name);
                string temp = "";
                foreach(var t in leagueInfo){
                    if (t.Position != "NONE" && t.Position != "APEX"){
                        temp += $"{(t.Position == "UTILITY" ? "Support" : (t.Position[0])+t.Position.Substring(1).ToLower())} - {t.Tier[0]+t.Tier.Substring(1).ToLower() , 7} {t.Rank}\n";
                        //temp += $" WR: {(t.Wins / (t.Wins + t.Losses))*100}%\n";
                    }
                    else if (t.Position == "APEX"){
                        temp += $"Solo Queue - {t.Tier[0]+t.Tier.Substring(1).ToLower()}\n";

                    }
                }
                field.WithValue(temp);
                fieldsX.Add(field);
            }
            catch(Exception ex){
                await Logger(new LogMessage(LogSeverity.Debug, "Search Names", $"Caught exception in searching names - {ex.GetType()}, {ex.Message}", ex));
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                field.WithName(n);
                field.WithValue("Does not exist");
                fieldsX.Add(field);                
            }
        }
        
        EmbedBuilder embeddedMessage = new EmbedBuilder();
        embeddedMessage.WithTitle("");
        embeddedMessage.WithThumbnailUrl($"http://ddragon.leagueoflegends.com/cdn/{DDragonTailVer}/img/profileicon/{toprint[0].ProfileIconId}.png");
        embeddedMessage.WithColor(0xff69b4);
        embeddedMessage.WithFields(fieldsX);
        await ReplyAsync("", embed:embeddedMessage.Build());
    }
    
    [Command("ddver"), RequireOwner]
    public async Task ddver(){
        await ReplyAsync(DDragonTailVer);
    }
    
}
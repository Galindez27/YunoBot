using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System;
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
    
    [Group("admin"), RequireUserPermission(GuildPermission.Administrator),  Name("Admin")]
    [Summary("Server admin commands. Can only be called by admins of a server.")]
    public class adminCommands : ModuleBase<SocketCommandContext>{
        
        [Command("list"), Summary("list the admins and lieutenants of a server")]
        public async Task listInfo(){
            await Context.Guild.DownloadUsersAsync();
            HashSet<SocketRole> roles = new HashSet<SocketRole>(Context.Guild.Roles);
            HashSet<SocketRole> nImportant = new HashSet<SocketRole>();
            foreach (SocketRole r in roles){
                if (r.Permissions.Administrator || r.Permissions.BanMembers || r.Permissions.ManageGuild
                || r.Permissions.ManageRoles || r.Permissions.ManageWebhooks || r.Permissions.SendTTSMessages
                || r.Permissions.DeafenMembers || r.Permissions.MentionEveryone){
                    nImportant.Add(r);
                } 
            }
            EmbedBuilder toEmbed = new EmbedBuilder();
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            toEmbed.WithColor(CommandHandlingService.embedColor);
            toEmbed.WithTitle(Context.Guild.Name + "\nImportant People");
            toEmbed.ThumbnailUrl = Context.Guild.IconUrl;
            
            foreach (var user in Context.Guild.Users){
                EmbedFieldBuilder field = new EmbedFieldBuilder();
                bool isImportant = false;
                field.Name = user.Username;
                field.Value = "```";
                foreach (var role in user.Roles){
                    if (nImportant.Contains(role)){
                        isImportant = true;
                    }
                    if (!role.IsEveryone) field.Value += role.Name + ", ";
                }
                field.Value += "```";
                if (isImportant){
                    fields.Add(field);
                }
            }
            toEmbed.WithFields(fields);
            
            await ReplyAsync(embed:toEmbed.Build());
        }
    }
}
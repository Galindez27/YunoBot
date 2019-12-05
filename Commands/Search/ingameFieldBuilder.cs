using Discord;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.SpectatorV4;

namespace YunoBot.Commands{
    public partial class Search{
        private EmbedFieldBuilder ingameFieldBuilder(CurrentGameParticipant par){
            EmbedFieldBuilder toReturn = new EmbedFieldBuilder().WithIsInline(true);
            string champ = ((Champion)par.ChampionId).ToString();
            toReturn.Name = $"{(par.TeamId == 100 ? ":small_red_triangle:":":small_blue_diamond:")}" + champ[0] + champ.Substring(1).ToLowerInvariant();
            toReturn.Value = par.SummonerName + "\n";
            return toReturn;
        }
    }
}
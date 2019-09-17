using MingweiSamuel.Camille.Enums;

namespace YunoBot.Commands{
    public partial class Search{
        private string championPictureUrl(Champion champ){
            switch (champ){
                case Champion.KAI_SA:
                    return string.Format(champArtUrlBase, "Kaisa");
                case Champion.NUNU_WILLUMP:
                    return string.Format(champArtUrlBase, "Nunu");
                default:
                    return string.Format(champArtUrlBase, champ.Name());                                
            }
        }
    }
}
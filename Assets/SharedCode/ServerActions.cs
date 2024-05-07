using Game.Logic;
using Metaplay.Core.Player;
using Metaplay.Core.Model;
using System;

namespace Game.Logic 
{
    [ModelAction(5001)]
    public class ServerMakeNumberGoUp : PlayerSynchronizedServerActionCore<PlayerModel>
    {
        public override MetaActionResult Execute(PlayerModel player, bool commit)
        {
            if (commit)
            {
                player.NumClicks += 1;
            }
            return ActionResult.Success;
        }
    }
}

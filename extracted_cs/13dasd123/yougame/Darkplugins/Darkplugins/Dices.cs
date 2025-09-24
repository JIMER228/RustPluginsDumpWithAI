using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Dices","Baks","0.0.1")]
    public class Dices : RustPlugin
    {
        [ChatCommand("dice")]
        void PlayerDice(BasePlayer player, string command, string[] args)
        {
            int x = Random.Range(1 - 6);
            SendReply(player,$"Вы бросили кубик и получили {x.ToString()}");
        }
    }
}
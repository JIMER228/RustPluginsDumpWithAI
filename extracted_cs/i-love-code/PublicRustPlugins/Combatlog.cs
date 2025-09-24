
namespace Oxide.Plugins
{
    [Info("Combatlog", "Jake_Rich", 1.0)]
    [Description("Get combatlog data on other players")]

    public class Combatlog : RustPlugin
    {
        [ConsoleCommand("getcombatlog")]
        void GetCombatlog_ChatCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                arg.ReplyWith("This command is for admins only!");
                return;
            }

            if (arg.Args.Length != 1)
            {
                arg.ReplyWith("Need 1 argument!");
                return;
            }

            ulong userID;
            if (!ulong.TryParse(arg.Args[0], out userID))
            {
                arg.ReplyWith($"{ arg.Args[0]} is not a valid userID!");
                return;
            }

            BasePlayer targetPlayer = BasePlayer.FindByID(userID);
            if (targetPlayer == null)
            {
                arg.ReplyWith($"{ userID} was not found!");
                return;
            }

            arg.ReplyWith(targetPlayer.stats.combat.Get(100));
        }
    }
}



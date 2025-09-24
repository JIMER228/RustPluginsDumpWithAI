namespace Oxide.Plugins
{
  using System;

  public partial class ZombieAttack : RustPlugin
  {
    void OnNumberCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 2)
      {
        SendReply(player, "Usage: <color=#ffd479>/za number MIN MAX</color>");
        return;
      }

      int min;
      int max;

      try
      {
        min = Convert.ToInt32(args[0]);
        max = Convert.ToInt32(args[1]);
      }
      catch
      {
        SendReply(player, "You must specify valid numbers for both minimum and maximum.");
        return;
      }

      if (min < 1 || max < 1 || min > MAX_ATTACKERS_ALLOWED || max > MAX_ATTACKERS_ALLOWED)
      {
        SendReply(player, $"Both minimum and maximum must be between 1 and {MAX_ATTACKERS_ALLOWED}.");
        return;
      }

      Settings.MinAttackers = min;
      Settings.MaxAttackers = max;

      SendReply(player, $"Attacks will now involve between <color=#ffd479>{min}</color> and <color=#ffd479>{max}</color> attackers.");
    }
  }
}
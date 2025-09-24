namespace Oxide.Plugins
{
  using System;

  public partial class ZombieAttack : RustPlugin
  {
    void OnIntervalCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 1)
      {
        SendReply(player, "Usage: <color=#ffd479>/za interval MINUTES</color>");
        return;
      }

      int intervalMinutes;

      try
      {
        intervalMinutes = Convert.ToInt32(args[0]);
      }
      catch
      {
        SendReply(player, "You must specify a valid number of minutes.");
        return;
      }

      if (intervalMinutes < 1)
      {
        SendReply(player, "The interval must be greater than 0.");
        return;
      }

      SendReply(player, $"Attacks will now occur every <color=#ffd479>{intervalMinutes}</color> minutes.");

      Settings.IntervalMinutes = intervalMinutes;
      StartTimer();
    }
  }
}
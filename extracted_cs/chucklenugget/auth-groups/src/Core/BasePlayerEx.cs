namespace Oxide.Plugins
{
  using System;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    public static class BasePlayerEx
    {
      public static BasePlayer FindByNameOrId(string searchString)
      {
        return FindById(searchString) ?? FindByName(searchString);
      }

      public static BasePlayer FindById(string id)
      {
        return BasePlayer.activePlayerList.Concat(BasePlayer.sleepingPlayerList)
          .FirstOrDefault(p => p.UserIDString == id);
      }

      public static BasePlayer FindByName(string searchString)
      {
        return BasePlayer.activePlayerList.Concat(BasePlayer.sleepingPlayerList)
          .Where(p => p.displayName.ToLowerInvariant().Contains(searchString.ToLowerInvariant()))
          .OrderBy(p => GetLevenshteinDistance(searchString.ToLowerInvariant(), p.displayName.ToLowerInvariant()))
          .FirstOrDefault();
      }

      static int GetLevenshteinDistance(string source, string target)
      {
        if (String.IsNullOrEmpty(source) && String.IsNullOrEmpty(target))
          return 0;

        if (source.Length == target.Length)
          return source.Length;

        if (source.Length == 0)
          return target.Length;

        if (target.Length == 0)
          return source.Length;

        var distance = new int[source.Length + 1, target.Length + 1];

        for (int idx = 0; idx <= source.Length; distance[idx, 0] = idx++) ;
        for (int idx = 0; idx <= target.Length; distance[0, idx] = idx++) ;

        for (int i = 1; i <= source.Length; i++)
        {
          for (int j = 1; j <= target.Length; j++)
          {
            int cost = target[j - 1] == source[i - 1] ? 0 : 1;
            distance[i, j] = Math.Min(
              Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
              distance[i - 1, j - 1] + cost
            );
          }
        }

        return distance[source.Length, target.Length];
      }
    }
  }
}

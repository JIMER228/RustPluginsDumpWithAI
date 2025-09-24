namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    static class Messages
    {
      public const string YouAreNotOwnerOrManagerOfGroup = "You are not the owner or manager of an auth group named <color=#ffd479>{0}</color>.";
      public const string NoSuchPlayer = "No player with the name or id <color=#ffd479>{0}</color> was found.";
      public const string InteractionCanceled = "Command canceled.";
      public const string InvalidEntitySelected = "That item cannot be added to an auth group.";

      public const string CannotCreateGroupOneAlreadyExists = "You are already the manager of an auth group named <color=#ffd479>{0}</color>!";
      public const string GroupCreated = "You have created the auth group <color=#ffd479>{0}</color>.";
      public const string GroupDeleted = "You have deleted the auth group <color=#ffd479>{0}</color>. The auth lists of all items in the group have been cleared.";

      public const string MemberAdded = "You have added <color=#ffd479>{0}</color> as a member of the auth group <color=#ffd479>{1}</color>.";
      public const string MemberRemoved = "You have removed <color=#ffd479>{0}</color> as a member of the auth group <color=#ffd479>{1}</color>.";
      public const string ManagerAdded = "You have added <color=#ffd479>{0}</color> as a manager of the auth group <color=#ffd479>{1}</color>.";
      public const string ManagerRemoved = "You have removed <color=#ffd479>{0}</color> as a manager of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotAddMemberAlreadyMemberOfGroup = "The player <color=#ffd479>{0}</color> is already a member of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotRemoveMemberNotMemberOfGroup = "The player <color=#ffd479>{0}</color> is not a member of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotPromoteNotMemberOfGroup = "The player <color=#ffd479>{0}</color> is not a member of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotPromoteAlreadyManagerOfGroup = "The player <color=#ffd479>{0}</color> is already a manager of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotDemoteNotManagerOfGroup = "The player <color=#ffd479>{0}</color> is not a manager of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotPromoteIsOwnerOfGroup = "The player <color=#ffd479>{0}</color> is the already the owner of the auth group <color=#ffd479>{1}</color>.";
      public const string CannotDemoteIsOwnerOfGroup = "The player <color=#ffd479>{0}</color> cannot be demoted, since they are the owner of the auth group <color=#ffd479>{1}</color>.";

      public const string SelectEntityToAddToGroup = "Use the hammer to select a tool cupboard or auto turret to add to the auth group <color=#ffd479>{0}</color>. Say <color=#ffd479>/ag</color> to cancel.";
      public const string SelectEntityToRemoveFromGroup = "Use the hammer to select a tool cupboard or auto turret to remove from the auth group <color=#ffd479>{0}</color>. Say <color=#ffd479>/ag</color> to cancel.";

      public const string EntityAdded = "You have added this item to the auth group <color=#ffd479>{0}</color>. <color=#ffd479>{1}</color> members have been authorized.";
      public const string EntityRemoved = "You have removed this item from the auth group <color=#ffd479>{0}</color>, and its auth list has been cleared.";
      public const string GroupSynchronized = "The auth list of all items in the group <color=#ffd479>{0}</color> has been cleared, and the <color=#ffd479>{1}</color> members of the group have been re-added.";

      public const string CannotSendTurretsCommandNoTurretsInGroup = "There are no turrets in the group <color=#ffd479>{0}</color>!";
      public const string AllTurretsSetOn = "All turrets in the auth group <color=#ffd479>{0}</color> have been turned on.";
      public const string AllTurretsSetOff = "All turrets in the auth group <color=#ffd479>{0}</color> have been turned off.";
      public const string AllTurretsSetHostile = "All turrets in the auth group <color=#ffd479>{0}</color> have been turned hostile.";
      public const string AllTurretsSetPeacekeeper = "All turrets in the auth group <color=#ffd479>{0}</color> have been turned to peacekeeper mode.";
      public const string AllTurretsLocked = "All turrets in the group <color=#ffd479>{0}</color> are now locked, and will only be accessible by group managers.";
      public const string AllTurretsUnlocked = "All turrets in the group <color=#ffd479>{0}</color> are now unlocked.";

      public const string CannotSendLocksCommandNoLocksInGroup = "There are no codelocks in the group <color=#ffd479>{0}</color>!";
      public const string AllLocksCodeChanged = "The code for all codelocks in the auth group <color=#ffd479>{0}</color> has been changed.";
      public const string AllLocksGuestCodeChanged = "The guest code for all codelocks in the auth group <color=#ffd479>{0}</color> has been changed.";
      public const string AllLocksLocked = "All codelocks in the auth group <color=#ffd479>{0}</color> have been locked.";
      public const string AllLocksUnlocked = "All codelocks in the auth group <color=#ffd479>{0}</color> have been unlocked.";

      public const string CannotAddEntityNotAuthorized = "You must be authorized on the item to add it to the auth group.";
      public const string CannotAddEntityAlreadyEnrolledInGroup = "That item is already enrolled in the auth group <color=#ffd479>{0}</color>.";
      public const string CannotRemoveEntityNotAuthorized = "You must be authorized on the item to remove it from the auth group.";
      public const string CannotRemoveEntityNotEnrolledInGroup = "That item is not enrolled in the auth group <color=#ffd479>{0}</color>.";
    }
  }
}

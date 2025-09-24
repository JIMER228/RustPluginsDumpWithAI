using System.Collections.Generic;
using System.Linq;
using Network;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Builder", "st-little", "1.2.1")]
    [Description("This plugin upgrades the hammer and streamlines the builder's work.")]
    public class Builder : RustPlugin
    {
        #region Fields

        private const string PermissionAdmin = "builder.admin";
        private const string PermissionRotation = "builder.rotation";
        private const string PermissionInversion = "builder.inversion";
        private const string PermissionRemove = "builder.remove";
        private const string PermissionDowngrade = "builder.downgrade";

        private const string FoundationPrefab = "assets/prefabs/building core/foundation/foundation.prefab";
        private const string FloorPrefab = "assets/prefabs/building core/floor/floor.prefab";
        private const string WallPrefab = "assets/prefabs/building core/wall/wall.prefab";
        private const string WallHalfPrefab = "assets/prefabs/building core/wall.half/wall.half.prefab";
        private const string WallLowPrefab = "assets/prefabs/building core/wall.low/wall.low.prefab";
        private const string WallDoorwayPrefab = "assets/prefabs/building core/wall.doorway/wall.doorway.prefab";
        private const string WallWindowPrefab = "assets/prefabs/building core/wall.window/wall.window.prefab";

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Usage: /build <rotation|inversion|remove|downgrade> [<grade>]",
                ["NoBuildingIsAllowed"] = "You are not allowed to build here.",
                ["MustHaveHammer"] = "You must have a hammer in your hand to use this command.",
                ["NoTargetFound"] = "No target found.",
                ["NoEntityFound"] = "No entity found.",
                ["NotAuthorizedToUseCommands"] = "You do not have permission to use this command.",
                ["UnsupportedEntity"] = "This entity is not supported.",
                ["CannotDowngradeFromTwigs"] = "Cannot downgrade from Twigs.",
                ["InvalidGradeSpecified"] = "Invalid grade specified.",
                ["CannotDowngradeToSameOrHigherGrade"] = "Cannot downgrade to the same or higher grade.",
                ["YouCannotAffordThisDowngrade"] = "You cannot afford this downgrade.",
                ["BuildingBlockDowngraded"] = "Building block downgraded to {0}.",
            }, this);

            // Japanese
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "使用方法: /build <rotation|inversion|remove|downgrade> [<grade>]",
                ["NoBuildingIsAllowed"] = "ここに建物を建てることは許可されていない。",
                ["MustHaveHammer"] = "このコマンドを使うには、手にハンマーを持っていなければならない。",
                ["NoTargetFound"] = "ターゲットが見つからない。",
                ["NoEntityFound"] = "エンティティが見つからない。",
                ["NotAuthorizedToUseCommands"] = "このコマンドを使用する権限がない。",
                ["UnsupportedEntity"] = "このエンティティはサポートされていない。",
                ["CannotDowngradeFromTwigs"] = "ツイッグからダウングレードできません。",
                ["InvalidGradeSpecified"] = "無効なグレードが指定されました。",
                ["CannotDowngradeToSameOrHigherGrade"] = "同じかそれ以上のグレードにダウングレードできません。",
                ["YouCannotAffordThisDowngrade"] = "このダウングレードを支払うことができません。",
                ["BuildingBlockDowngraded"] = "建物のブロックが {0} にダウングレードされました。",
            }, this, "ja");
        }

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionAdmin, this);
            permission.RegisterPermission(PermissionRotation, this);
            permission.RegisterPermission(PermissionInversion, this);
            permission.RegisterPermission(PermissionRemove, this);
            permission.RegisterPermission(PermissionDowngrade, this);
        }

        #endregion

        #region Commands

        [ChatCommand("build")]
        private void BuildCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                SendReply(player, lang.GetMessage("CommandUsage", this, player.UserIDString));
                return;
            }

            // Check if the player has permission to build.
            var buildingPrivilege = player.GetBuildingPrivilege();
            if (buildingPrivilege == null)
            {
                SendReply(player, lang.GetMessage("NoBuildingIsAllowed", this, player.UserIDString));
                return;
            }

            // Check if the player is authorized to build.
            var authorizedPlayer = buildingPrivilege.authorizedPlayers.Any((p) =>
            {
                return p.userid == player.userID;
            });
            if (!authorizedPlayer)
            {
                SendReply(player, lang.GetMessage("NoBuildingIsAllowed", this, player.UserIDString));
                return;
            }

            if (player.GetActiveItem()?.info.shortname != "hammer" && player.GetActiveItem()?.info.shortname != "toolgun")
            {
                SendReply(player, lang.GetMessage("MustHaveHammer", this, player.UserIDString));
                return;
            }

            var hit = GetPlayerEyesHeadRay(player, 2.2f);
            if (hit == null)
            {
                SendReply(player, lang.GetMessage("NoTargetFound", this, player.UserIDString));
                return;
            }

            var entity = hit?.GetEntity();
            if (entity == null)
            {
                SendReply(player, lang.GetMessage("NoEntityFound", this, player.UserIDString));
                return;
            }
            var buildingBlock = entity as BuildingBlock;
            if (buildingBlock == null)
            {
                SendReply(player, lang.GetMessage("NoEntityFound", this, player.UserIDString));
                return;
            }

            switch (args[0].ToLower())
            {
                case "rotation":
                case "r":
                    if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && !permission.UserHasPermission(player.UserIDString, PermissionRotation))
                    {
                        SendReply(player, lang.GetMessage("NotAuthorizedToUseCommands", this, player.UserIDString));
                        return;
                    }
                    if (buildingBlock.name != FoundationPrefab && buildingBlock.name != FloorPrefab)
                    {
                        SendReply(player, lang.GetMessage("UnsupportedEntity", this, player.UserIDString));
                        return;
                    }
                    RotateEntity(buildingBlock, 90);
                    break;
                case "inversion":
                case "i":
                    if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && !permission.UserHasPermission(player.UserIDString, PermissionInversion))
                    {
                        SendReply(player, lang.GetMessage("NotAuthorizedToUseCommands", this, player.UserIDString));
                        return;
                    }
                    if (buildingBlock.name != WallPrefab && buildingBlock.name != WallHalfPrefab && buildingBlock.name != WallLowPrefab && buildingBlock.name != WallDoorwayPrefab && buildingBlock.name != WallWindowPrefab)
                    {
                        SendReply(player, lang.GetMessage("UnsupportedEntity", this, player.UserIDString));
                        return;
                    }
                    InversionEntity(buildingBlock);
                    break;
                case "remove":
                case "rm":
                    if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && !permission.UserHasPermission(player.UserIDString, PermissionRemove))
                    {
                        SendReply(player, lang.GetMessage("NotAuthorizedToUseCommands", this, player.UserIDString));
                        return;
                    }
                    RemoveEntity(buildingBlock);
                    break;
                case "downgrade":
                case "dg":
                    if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && !permission.UserHasPermission(player.UserIDString, PermissionDowngrade))
                    {
                        SendReply(player, lang.GetMessage("NotAuthorizedToUseCommands", this, player.UserIDString));
                        return;
                    }
                    var lastGrade = buildingBlock.lastGrade;
                    // Check if the last grade is Twigs, as it cannot be downgraded further.
                    if (lastGrade == BuildingGrade.Enum.Twigs)
                    {
                        SendReply(player, lang.GetMessage("CannotDowngradeFromTwigs", this, player.UserIDString));
                        return;
                    }
                    BuildingGrade.Enum toGrade;
                    // If args[1] is provided, try to parse it as a BuildingGrade.Enum.
                    if (args.Length >= 2)
                    {
                        if (!System.Enum.TryParse(args[1], true, out toGrade))
                        {
                            SendReply(player, lang.GetMessage("InvalidGradeSpecified", this, player.UserIDString));
                            return;
                        }
                    }
                    // If args[1] is not provided, set toGrade to one grade lower than lastGrade.
                    else
                    {
                        toGrade = lastGrade - 1;
                    }
                    // Check if the specified grade is valid.
                    if (!ConVar.Decay.CanUpgradeToGrade(toGrade))
                    {
                        SendReply(player, lang.GetMessage("InvalidGradeSpecified", this, player.UserIDString));
                        return;
                    }
                    // Check if the specified grade is lower than the last grade.
                    if (lastGrade <= toGrade)
                    {
                        SendReply(player, lang.GetMessage("CannotDowngradeToSameOrHigherGrade", this, player.UserIDString));
                        return;
                    }
                    // Check if the building block can be downgraded to the specified grade.
                    var constructionGrade = buildingBlock.blockDefinition.GetGrade(toGrade, 0);
                    if (constructionGrade == null)
                    {
                        SendReply(player, lang.GetMessage("InvalidGradeSpecified", this, player.UserIDString));
                        return;
                    }
                    // Check if the player can afford the downgrade.
                    var canAffordUpgrade = buildingBlock.CanAffordUpgrade(toGrade, 0, player);
                    if (!canAffordUpgrade)
                    {
                        SendReply(player, lang.GetMessage("YouCannotAffordThisDowngrade", this, player.UserIDString));
                        return;
                    }

                    buildingBlock.ChangeGradeAndSkin(toGrade, 0, player);
                    buildingBlock.PayForUpgrade(constructionGrade, player);
                    SendReply(player, lang.GetMessage("BuildingBlockDowngraded", this, player.UserIDString).Replace("{0}", toGrade.ToString()));
                    break;
                default:
                    SendReply(player, lang.GetMessage("CommandUsage", this, player.UserIDString));
                    break;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Entity regeneration.
        /// </summary>
        /// <param name="buildingBlock">regenerating entity</param>
        private static void EntityRegeneration(BuildingBlock buildingBlock)
        {
            using (NetWrite write = buildingBlock.net.sv.StartWrite())
            {
                write.PacketID(Message.Type.EntityDestroy);
                write.UInt64(buildingBlock.net.ID.Value);
                write.UInt8(0);
                write.Send(new SendInfo(buildingBlock.net.group.subscribers));
            }
            buildingBlock.SendNetworkUpdateImmediate();
        }

        /// <summary>
        /// Rotate the entity to the right.
        /// </summary>
        /// <param name="buildingBlock">rotating entity</param>
        /// <param name="angle">Angle of rotation</param>
        private static void RotateEntity(BuildingBlock buildingBlock, float angle)
        {
            var angles = buildingBlock.transform.eulerAngles;
            buildingBlock.transform.eulerAngles = new Vector3(angles.x, angles.y + angle, angles.z);
            EntityRegeneration(buildingBlock);
        }

        /// <summary>
        /// Invert the entity.
        /// </summary>
        /// <param name="buildingBlock">Inverting entity</param>
        private static void InversionEntity(BuildingBlock buildingBlock)
        {
            var rotation = buildingBlock.transform.rotation;
            buildingBlock.transform.Rotate(new Vector3(rotation.x, rotation.y + 180, rotation.z));
            EntityRegeneration(buildingBlock);
        }

        private static void RemoveEntity(BuildingBlock buildingBlock)
        {
            buildingBlock.Kill();
        }

        private static RaycastHit? GetPlayerEyesHeadRay(BasePlayer basePlayer, float maxDistance = Mathf.Infinity)
        {
            return Physics.Raycast(basePlayer.eyes.HeadRay(), out var hit, maxDistance, Physics.DefaultRaycastLayers)
                ? hit
                : null;
        }

        #endregion
    }
}


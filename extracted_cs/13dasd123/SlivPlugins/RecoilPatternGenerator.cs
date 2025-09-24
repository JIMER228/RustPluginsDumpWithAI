using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Recoil Pattern Generator", "Amino", "0.1.0")]
    [Description("Helps to capture and generate recoil and anti-recoil patterns of any weapon with any attachments accurately.")]
    public class RecoilPatternGenerator : RustPlugin
    {
        private BasePlayer _activePlayer;
        private float _firstShot = 0;
        private ulong _weaponId;
        private float _lastShootTime;
        private DynamicConfigFile _dataManager;
        PluginData _pluginData = new PluginData();
        private RecoilData _currentRecoilData;
        private const int Border = 135;
        private readonly List<float> _shots = new List<float>();
        private readonly List<List<float>> _totalShots = new List<List<float>>();
        private bool _isRecording;
        private void Loaded()
        {
            _dataManager = Interface.Oxide.DataFileSystem.GetFile(nameof(RecoilPatternGenerator));
            _pluginData = _dataManager.ReadObject<PluginData>();
            _isRecording = false;
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProjectileShoot projectileShoot)
        {
            if (!_isRecording || projectile == null || player == null)
            {
                return;
            }

            if (_weaponId != projectile.prefabID)
            {
                player.ChatMessage("You are shooting with a wrong weapon");
                return;
            }
            if (Time.realtimeSinceStartup > _lastShootTime + 1f)
            {
                RegisterShots();
            }
            _lastShootTime = Time.realtimeSinceStartup;

            RecordShot();
        }
        private void RecordShot()
        {
            if (_activePlayer == null)
                return;

            var current = _activePlayer.eyes.rotation.eulerAngles.y;
            var value = 0f;
            if (_shots.Count == 0)
            {
                _firstShot = current;
                _shots.Add(0);
                return;
            }

            if (_firstShot > Border)
            {
                if (current < Border)
                {
                    value = 360 - _firstShot + current;
                }
                else
                {
                    value = current - _firstShot;
                }
            }
            else
            {
                if (current > Border)
                {
                    value = -(360 - current);
                }
                else
                {
                    value = current - _firstShot;
                }
            }
            value *= 10;
            _shots.Add(Convert.ToInt32(value));
        }
        //recoil.ready
        [ConsoleCommand("recoil.ready")]
        private void ClearAnglesBuffer(ConsoleSystem.Arg conArg)
        {
            var player = conArg.Player();
            if (player == null || !player.IsAdmin)
            {
                return;
            }

            if (_activePlayer != null)
            {
                player.ChatMessage("There is an active player for recording shots.");
                return;
            }

            _activePlayer = player;
            player.inventory.containerBelt.SetLocked(true);
            var weapon = player.GetHeldEntity() as BaseProjectile;
            if (weapon == null)
            {
                player.ChatMessage("You don't hold any projectile weapon.");
                return;
            }

            _weaponId = weapon.prefabID;
            _firstShot = 0;
            _shots.Clear();
            _totalShots.Clear();
            _currentRecoilData = new RecoilData();
            _isRecording = true;

            var heldEntity = player.GetHeldEntity();
            if (heldEntity != null)
            {
                var heldItem = heldEntity.GetItem();
                if (heldItem != null)
                {
                    var items = heldItem.contents?.itemList;
                    if (items != null)
                    {
                        var tempRecoilData = new RecoilData();
                        foreach (var subItem in items)
                        {
                            if (subItem != null && subItem.info != null)
                                tempRecoilData.Attachments.Add(new AttachmentData
                                {
                                    ItemShortName = subItem.info.shortname
                                });
                        }
                        tempRecoilData.WeaponName = weapon.ShortPrefabName;
                        _currentRecoilData = tempRecoilData;
                        if (!_pluginData.Recoils.Any(a => a.Equals(tempRecoilData)))
                        {
                            _pluginData.Recoils.Add(tempRecoilData);
                        }
                    }
                }
            }

            player.ChatMessage("Buffer cleared. You can start shooting");
        }

        //recoil.generate
        [ConsoleCommand("recoil.generate")]
        private void GenerateRecoilPatternCommand(ConsoleSystem.Arg conArg)
        {
            var player = conArg.Player();
            if (player == null || !player.IsAdmin)
            {
                return;
            }
            _activePlayer = null;
            player.inventory.containerBelt.SetLocked(false);
            RegisterShots();
            var max = _totalShots.Count > 0 ? _totalShots.Max(x => x.Count) : 0;
            PrintWarning($"Max shots: {max}");
            for (var i = 0; i < _totalShots.Count; i++)
            {
                PrintWarning($"{i}: {string.Join(",", _totalShots[i])}");
            }
            for (var i = 0; i < max; i++)
            {
                var sum = 0f;
                var count = 0;
                foreach (var shot in _totalShots)
                {
                    if (shot.Count <= i)
                        continue;
                    sum += shot[i];
                    count++;
                }
                _shots.Add(Convert.ToInt32(sum / count));
            }
            PrintWarning($"AVG: {string.Join(",", _shots)}");
            var dataIndex = _pluginData.Recoils.IndexOf(_currentRecoilData);
            if (dataIndex >= 0)
            {
                _pluginData.Recoils[dataIndex].RecoilPoints = _shots.Select(Convert.ToInt32).ToList();
            }
            _dataManager.WriteObject(_pluginData);
            _firstShot = 0;
            _weaponId = 0;
            _shots.Clear();
            _totalShots.Clear();
            _isRecording = false;
            player.ChatMessage("Recoil pattern generated");
        }

        private void RegisterShots()
        {
            if (_shots.Count > 0)
            {
                _totalShots.Add(new List<float>(_shots));
                _shots.Clear();
            }
        }
        public class AttachmentData
        {
            [JsonProperty(PropertyName = "Item Short Name")]
            public string ItemShortName { get; set; } = "";
        }
        public class RecoilData
        {
            [JsonProperty(PropertyName = "Weapon Short Name")]
            public string WeaponName { get; set; } = "";

            [JsonProperty(PropertyName = "Attachments")]
            public List<AttachmentData> Attachments { get; set; } = new List<AttachmentData>();

            [JsonProperty(PropertyName = "Recoil Points")]
            public List<int> RecoilPoints { get; set; } = new List<int>();
            public override bool Equals(object obj)
            {
                var targetData = obj as RecoilData;
                if (targetData == null)
                    return false;
                return targetData.GetHashCode() == GetHashCode();
            }
            public override int GetHashCode()
            {
                if (!Attachments.Any())
                    return WeaponName.GetHashCode();
                var currentAttachments = Attachments
                    .OrderBy(x => x.ItemShortName)
                   .Select(a => a.ItemShortName)
                   .Aggregate((a1, a2) => $"{a1}-{a2}");
                currentAttachments = $"{currentAttachments}-{WeaponName}";
                return currentAttachments.GetHashCode();
            }
        }
        public class PluginData
        {
            [JsonProperty(PropertyName = "Recoils")]
            public List<RecoilData> Recoils { get; set; } = new List<RecoilData>();
        }
    }
}

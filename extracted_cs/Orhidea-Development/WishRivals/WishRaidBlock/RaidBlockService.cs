using Oxide.Core;
using Oxide.Core.Configuration;
using System;

namespace Oxide.Plugins
{
    public class RaidBlockService
    {
        private readonly DynamicConfigFile _config;

        private bool _isForceActive;
        private bool _isActive;
        public RaidBlockService(DynamicConfigFile config)
        {
            _config = config;
        }

        public bool IsOn()
        {
            if (_isForceActive) return true;

            if ((bool)_config["RaidBlockOn"] == false) return false;

            return _isActive;
            //return IsOnUsingConfigTime();
        }
        public bool IsForceActivated()
        {
            return _isForceActive;
        }
        public void Enable()
        {
            _isForceActive = true;
        }
        public void Disable()
        {
            _isForceActive = false;
        }

        public void UpdateIsOnUsingConfigTime()
        {
            var start = GetStartTime();
            var end = GetEndTime();
            var now = GetLatvianTime().TimeOfDay;

            if (start <= end)
            {
                _isActive = now >= start && now <= end;
            }
            _isActive = now >= start || now <= end;
        }

        public TimeSpan GetStartTime()
        {
            return GetTimeFromConfig("RaidBlockStart");

        }
        public TimeSpan GetEndTime()
        {
            return GetTimeFromConfig("RaidBlockEnd");
        }

        private TimeSpan GetTimeFromConfig(string key)
        {
            try
            {
                var time = DateTime.Parse(_config[key].ToString()).TimeOfDay;
                Interface.Oxide.LogDebug($"Raidblock: Read time from config {time}");
                return time;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error GetTimeFromConfig {key}. {key}", ex);
            }
        }

        private static DateTime GetLatvianTime()
        {
            var currentTime = DateTime.Now.AddHours(1);
            Interface.Oxide.LogDebug($"Got latvian time: {currentTime.TimeOfDay}");
            return currentTime;
        }
    }
}

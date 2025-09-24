namespace Oxide.Plugins
{
    using UnityEngine;
    using System.Collections.Generic;
    using Network;

    public interface IFactionsMapMarkerSpecification
    {
        Vector2 GetLocation();
    }

    public sealed class ClanClaim : IFactionsMapMarkerSpecification
    {
        private readonly Vector2 _location;
        public readonly Color Color;
        public readonly bool IsCapital;

        public ClanClaim(float colorRed, float colorGreen, float colorBlue, bool isCapital, Vector2 location)
        {
            Color = new Color(colorRed, colorGreen, colorBlue);
            _location = location;
            IsCapital = isCapital;
        }

        Vector2 IFactionsMapMarkerSpecification.GetLocation()
        {
            return _location;
        }
    }

    public sealed class Badlands : IFactionsMapMarkerSpecification
    {
        private readonly Vector2 _location;

        public Badlands()
        {
            // TODO
        }

        Vector2 IFactionsMapMarkerSpecification.GetLocation()
        {
            return _location;
        }
    }

    public sealed class FactionsMapMarker
    {
        private MapMarkerGenericRadius _marker;
        private Timer _pendingTimer = null;

        private static class Constants
        {
            public const string EntityPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";
            public const string MarkerUpdateRpcFunction = "MarkerUpdate";
            public const float ClanClaimAlpha = 0.5f;
            public const float SubscriptionAddDelaySeconds = 10f;
        }

        public FactionsMapMarker(IFactionsMapMarkerSpecification specification)
        {
            Vector2 position = specification.GetLocation();
            _marker = GameManager.server.CreateEntity(Constants.EntityPrefab, position)
                .GetComponent<MapMarkerGenericRadius>();
            
            var claim = (ClanClaim)specification;
            if (claim != null)
            {
                _marker.color1 = claim.Color;
                _marker.radius = 2f;
                _marker.alpha = Constants.ClanClaimAlpha;
            }

            _marker.Spawn();
        }

        public void Respawn()
        {
            var prevColor = _marker.color1;
            var prevRadius = _marker.radius;
            var prevAlpha = _marker.alpha;
            _marker.Kill();
            _marker = GameManager.server.CreateEntity(Constants.EntityPrefab, _marker.ServerPosition)
                .GetComponent<MapMarkerGenericRadius>();
            _marker.color1 = prevColor;
            _marker.radius = prevRadius;
            _marker.alpha = prevAlpha;
            _marker.Spawn();
        }

        public void PlayerSubscriptionAdd(BasePlayer player, PluginTimers timer)
        {
            _marker.OnNetworkSubscribersEnter(new List<Connection>() { player.Connection });
            // Adding network subscribers is an async queue, so sending the update must be on a slight delay
            _pendingTimer?.Destroy();
            _pendingTimer = timer.Once(Constants.SubscriptionAddDelaySeconds, () =>
            {
                _marker.SendUpdate();
            });
        }

        public void PlayerSubscriptionRemove(BasePlayer player)
        {
            _marker.OnNetworkSubscribersLeave(new List<Connection>() { player.Connection });
        }

        public void Kill()
        {
            _marker.Kill();
        }
    }
}

using UnityEngine;

namespace Oxide.Plugins
{
    using System;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    internal class FactionsClaimableLand : IFactionsClaimableLand
    {
        [JsonProperty(PropertyName = "column")]
        private readonly int _column;
        [JsonProperty(PropertyName = "row")]
        private readonly int _row;
        [JsonProperty(PropertyName = "claimant_faction_id")]
        private int? _claimantFactionId;

        FactionsClaimableLand(int column, int row, int? claimantFactionId)
        {
            _column = column;
            _row = row;
            _claimantFactionId = claimantFactionId;
        }

        int? IFactionsClaimableLand.GetClaimantFactionId()
        {
            return _claimantFactionId;
        }

        int IFactionsClaimableLand.GetColumn()
        {
            return _column;
        }

        int IFactionsClaimableLand.GetRow()
        {
            return _row;
        }

        bool IFactionsClaimableLand.IsClaimed()
        {
            return _claimantFactionId != null;
        }

        void IFactionsClaimableLand.UpdateClaimantFactionId(int? claimantFactionId)
        {
            _claimantFactionId = claimantFactionId;
        }
    }
}

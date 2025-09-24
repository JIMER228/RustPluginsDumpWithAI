// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
    interface IFactionsClaimableLand
    {
        int GetColumn();
        int GetRow();
        bool IsClaimed();
        int? GetClaimantFactionId();
        void UpdateClaimantFactionId(int? claimantFactionId);
    }
}

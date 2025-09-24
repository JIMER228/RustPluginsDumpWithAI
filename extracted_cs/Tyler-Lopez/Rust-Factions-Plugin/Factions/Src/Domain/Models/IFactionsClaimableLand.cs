namespace Oxide.Plugins
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

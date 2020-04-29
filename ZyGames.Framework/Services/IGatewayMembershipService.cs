using ZyGames.Framework.Services.Membership;

namespace ZyGames.Framework.Services
{
    public interface IGatewayMembershipService : ISystemTarget
    {
        [OperationContract(InvokeMethodOptions.OneWay)]
        void MembershipTableChanged(MembershipVersion version);

        [OperationContract(InvokeMethodOptions.OneWay)]
        void KillService(Identity identity);
    }
}

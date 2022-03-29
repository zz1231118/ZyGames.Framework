using ZyGames.Framework.Services.Membership;

namespace ZyGames.Framework.Services
{
    [SystemTargetContract(Priority.Core)]
    public interface IClusterMembershipService : ISystemTarget
    {
        [OperationContract]
        bool Alive(MembershipEntry membershipEntry);

        [OperationContract]
        MembershipSnapshot Register(MembershipTable membershipTable);

        [OperationContract]
        void Unregister(MembershipEntry membershipEntry);

        [OperationContract(InvokeMethodOptions.OneWay)]
        void MembershipTableChanged(MembershipTable membershipTable);

        [OperationContract]
        MembershipSnapshot CreateMembershipSnapshot();

        [OperationContract(InvokeMethodOptions.OneWay)]
        void KillService(Identity identity);
    }
}

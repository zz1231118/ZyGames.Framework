using ZyGames.Framework.Services.Membership;

namespace ZyGames.Framework.Services
{
    public interface IClusterMembershipService : ISystemTarget
    {
        [OperationContract]
        bool Alive(MembershipEntry entry);

        [OperationContract]
        MembershipSnapshot Register(MembershipTable table);

        [OperationContract]
        void Unregister(MembershipEntry entry);

        [OperationContract(InvokeMethodOptions.OneWay)]
        void TableChanged(MembershipTable table);

        [OperationContract]
        MembershipSnapshot CreateSnapshot();

        [OperationContract(InvokeMethodOptions.OneWay)]
        void KillService(Identity identity);
    }
}

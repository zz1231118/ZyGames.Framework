using ZyGames.Framework.Services;

namespace SimpleFrameworkTest
{
    public interface IAbstractService : IService
    {
        [OperationContract]
        string GetName();
    }
}

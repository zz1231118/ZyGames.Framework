namespace ZyGames.Framework.Services
{
    /// <summary>
    /// 可远程访问的地址接口
    /// </summary>
    public interface IAddressable
    {
        /// <summary>
        /// 地址
        /// </summary>
        Address Address { get; }

        /// <summary>
        /// 身份标识
        /// </summary>
        Identity Identity { get; }
    }
}

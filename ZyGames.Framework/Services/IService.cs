using System;

namespace ZyGames.Framework.Services
{
    /// <summary>
    /// 服务接口
    /// </summary>
    public interface IService : IAddressable
    {
        /// <summary>
        /// 是否存活
        /// </summary>
        bool IsAlived { get; }

        /// <summary>
        /// 元数据
        /// </summary>
        object Metadata { get; }

        /// <summary>
        /// 获取指定类型的元数据
        /// </summary>
        T GetMeta<T>() where T : class;

        /// <summary>
        /// 获取指定类型的元数据
        /// </summary>
        [Obsolete("Use T GetMeta<T>()")]
        T GetMetadata<T>() where T : class;
    }

    //public interface IServiceWithGuidKey : IService
    //{ }

    //public interface IServiceWithIntegerKey : IService
    //{ }

    //public interface IServiceWithStringKey : IService
    //{ }
}
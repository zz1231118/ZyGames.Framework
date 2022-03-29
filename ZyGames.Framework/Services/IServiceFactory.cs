using System;
using System.Collections.Generic;
using Framework.Injection;
using ZyGames.Framework.Services.Lifecycle;

namespace ZyGames.Framework.Services
{
    /// <summary>
    /// 服务工厂接口
    /// </summary>
    public interface IServiceFactory
    {
        /// <summary>
        /// 容器
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// 成员生命周期管理器
        /// </summary>
        IMembershipLifecycle MembershipLifecycle { get; }

        /// <summary>
        /// 创建一个新的服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="serviceInterfaceType">服务接口类型</param>
        /// <param name="identity">身份标识</param>
        /// <param name="metadata">元数据</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        IService NewService(Type serviceType, Type serviceInterfaceType, Identity identity = null, object metadata = null);

        /// <summary>
        /// 创建一个新的服务
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <typeparam name="TService">服务泛型模板</typeparam>
        /// <param name="identity">身份标识</param>
        /// <param name="metadata">元数据</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        TServiceInterface NewService<TServiceInterface, TService>(Identity identity = null, object metadata = null)
            where TServiceInterface : IService
            where TService : Service, TServiceInterface;

        /// <summary>
        /// 获取指定身份标识的服务
        /// </summary>
        /// <param name="identity">身份标识</param>
        /// <param name="throwOnError">如果未找到是否抛出异常。默认：true。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ServiceNotFoundException" />
        IService GetService(Identity identity, bool throwOnError = true);

        /// <summary>
        /// 获取指定类型的服务集合
        /// </summary>
        /// <param name="serviceInterfaceType">服务接口类型</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        IReadOnlyList<IService> GetServices(Type serviceInterfaceType);

        /// <summary>
        /// 获取指定身份标识的服务
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <param name="identity">身份标识</param>
        /// <param name="throwOnError">如果未找到是否抛出异常。默认：true。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ServiceNotFoundException" />
        TServiceInterface GetService<TServiceInterface>(Identity identity, bool throwOnError = true)
            where TServiceInterface : IService;

        /// <summary>
        /// 获取首个指定类型的服务
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <param name="throwOnError">如果未找到是否抛出异常。默认：true。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        TServiceInterface GetService<TServiceInterface>(bool throwOnError = true)
            where TServiceInterface : IService;

        /// <summary>
        /// 获取指定类型的服务集合
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <returns></returns>
        IReadOnlyList<TServiceInterface> GetServices<TServiceInterface>()
            where TServiceInterface : IService;

        /// <summary>
        /// 查找符合指定条件的服务
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        TServiceInterface GetService<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService;

        /// <summary>
        /// 查找符合指定条件的服务集合
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        IReadOnlyList<TServiceInterface> GetServices<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService;

        /// <summary>
        /// 查找符合指定条件的服务
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        [Obsolete("Use TServiceInterface GetService<TServiceInterface>(Func<TServiceInterface, bool> predicate)")]
        TServiceInterface FindService<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService;

        /// <summary>
        /// 查找符合指定条件的服务集合
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        [Obsolete("Use IReadOnlyList<TServiceInterface> GetServices<TServiceInterface>(Func<TServiceInterface, bool> predicate)")]
        IReadOnlyList<TServiceInterface> FindAllService<TServiceInterface>(Func<TServiceInterface, bool> predicate)
            where TServiceInterface : IService;

        /// <summary>
        /// 获取指定类型的唯一服务，如果该类型服务并非恰好包含一个元素，则会引发异常。
        /// </summary>
        /// <typeparam name="TServiceInterface">服务接口泛型模板</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException" />
        TServiceInterface GetSingleService<TServiceInterface>()
            where TServiceInterface : IService;

        /// <summary>
        /// 杀死指定身份标识的服务
        /// </summary>
        /// <param name="identity">身份标识</param>
        /// <exception cref="ArgumentNullException" />
        void KillService(Identity identity);

        /// <summary>
        /// 创建指定对象的引用
        /// </summary>
        /// <typeparam name="TServiceObserverInterface">服务观察者接口泛型模板</typeparam>
        /// <param name="obj">欲创建的对象</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException" />
        TServiceObserverInterface CreateObjectReference<TServiceObserverInterface>(IServiceObserver obj)
            where TServiceObserverInterface : IServiceObserver;

        /// <summary>
        /// 删除指定对象的引用
        /// </summary>
        /// <param name="obj">欲删除的对象</param>
        /// <exception cref="ArgumentNullException" />
        void DeleteObjectReference(IServiceObserver obj);
    }
}

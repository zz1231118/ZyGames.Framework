using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZyGames.Framework.Services;

namespace SwordWorld.GameServers.Services
{
    public class DataService : Service, IDataService
    {
        private readonly Dictionary<Type, IEntityGroup> dictionary = new Dictionary<Type, IEntityGroup>();

        private IEntityGroup<T> GetEntityGroup<T>()
        {
            var primaryKey = typeof(T);
            if (!dictionary.TryGetValue(primaryKey, out IEntityGroup entityGroup))
            {
                entityGroup = new EntityGroup<T>();
                dictionary[primaryKey] = entityGroup;
            }
            return (IEntityGroup<T>)entityGroup;
        }

        public Task<T> Find<T>(Func<T, bool> predicate)
        {
            throw new NotImplementedException();
        }

        interface IEntityGroup
        {

        }

        interface IEntityGroup<T> : IEntityGroup
        { }

        class EntityGroup<T> : Dictionary<long, T>, IEntityGroup<T>
        {

        }
    }
}

using System;
using System.Collections.Concurrent;

namespace ZyGames.Framework.Services.Directory
{
    internal class AddressableDirectory
    {
        private readonly ConcurrentDictionary<Identity, IAddressable> addressables = new ConcurrentDictionary<Identity, IAddressable>();

        public void Add(IAddressable addressable)
        {
            addressables[addressable.Identity] = addressable;
        }

        public bool Remove(Identity identity)
        {
            return addressables.TryRemove(identity, out _);
        }

        public IAddressable GetAddressable(Identity identity)
        {
            addressables.TryGetValue(identity, out IAddressable service);
            return service;
        }

        public IAddressable GetAddressable(Identity identity, Func<Identity, IAddressable> valueFactory)
        {
            return addressables.GetOrAdd(identity, valueFactory);
        }

        public bool TryGetAddressable(Identity identity, out IAddressable addressable)
        {
            return addressables.TryGetValue(identity, out addressable);
        }
    }
}
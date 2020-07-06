using System;
using System.Collections.Generic;

namespace ZyGames.Framework.Services.Directory
{
    internal class AddressableDirectory
    {
        private readonly object lockable = new object();
        private readonly Dictionary<Identity, IAddressable> addressables = new Dictionary<Identity, IAddressable>();

        public void Add(IAddressable addressable)
        {
            lock (lockable)
            {
                addressables[addressable.Identity] = addressable;
            }
        }

        public bool Remove(Identity identity)
        {
            lock (lockable)
            {
                return addressables.Remove(identity);
            }
        }

        public IAddressable GetAddressable(Identity identity)
        {
            lock (lockable)
            {
                addressables.TryGetValue(identity, out IAddressable service);
                return service;
            }
        }

        public IAddressable GetAddressable(Identity identity, Func<Identity, IAddressable> valueFactory)
        {
            lock (lockable)
            {
                if (!addressables.TryGetValue(identity, out IAddressable addressable))
                {
                    addressable = valueFactory(identity);
                    addressables[identity] = addressable;
                }

                return addressable;
            }
        }

        public bool TryGetAddressable(Identity identity, out IAddressable addressable)
        {
            lock (lockable)
            {
                return addressables.TryGetValue(identity, out addressable);
            }
        }
    }
}
using System;

namespace ZyGames.Framework.Services
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class OperationContractAttribute : Attribute
    {
        private int requestTimeout = Constants.RequestTimeout.None;

        public OperationContractAttribute()
        { }

        public OperationContractAttribute(InvokeMethodOptions options)
        {
            Options = options;
        }

        public InvokeMethodOptions Options { get; set; }

        public int RequestTimeout
        {
            get => requestTimeout;
            set 
            {
                if (value <= Constants.RequestTimeout.None)
                    throw new ArgumentOutOfRangeException(nameof(value));

                requestTimeout = value;
            }
        }
    }
}
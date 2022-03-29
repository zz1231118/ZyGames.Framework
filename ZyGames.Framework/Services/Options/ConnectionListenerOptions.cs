using System;
using Framework.Configuration;
using Framework.Net.Sockets;

namespace ZyGames.Framework.Services.Options
{
    public class ConnectionListenerOptions
    {
        private Address insideAddress;
        private Address outsideAddress;

        /// <summary>
        /// 内部地址。
        /// </summary>
        public Address InsideAddress
        {
            get => insideAddress;
            set => insideAddress = value;
        }

        /// <summary>
        /// 外部地址。
        /// </summary>
        public Address OutsideAddress
        {
            get => outsideAddress ?? insideAddress;
            set => outsideAddress = value;
        }

        /// <summary>
        /// 最大挂起连接队列的长度。
        /// </summary>
        public int Backlog { get; set; }

        /// <summary>
        /// 最大连接数。
        /// </summary>
        public int MaxConnections { get; set; }

        /// <summary>
        /// 缓存大小。默认：2048
        /// </summary>
        public int BufferSize { get; set; } = SocketConstants.DefaultBufferSize;

        /// <summary>
        /// 请求超时时间。默认：30s
        /// </summary>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 最高负载。默认：0
        /// <para>0：无限制。</para>
        /// </summary>
        public int Overloaded { get; set; }

        /// <summary>
        /// 从指定配置项中读取配置。
        /// </summary>
        public virtual void CopyFrom(Config conf)
        {
            if (conf == null)
                throw new ArgumentNullException(nameof(conf));

            insideAddress = new Address(conf.GetString(nameof(InsideAddress)));
            if (conf.HasPath(nameof(OutsideAddress)))
            {
                outsideAddress = new Address(conf.GetString(nameof(OutsideAddress)));
            }

            Backlog = conf.GetInt32(nameof(Backlog));
            MaxConnections = conf.GetInt32(nameof(MaxConnections));
            BufferSize = conf.GetInt32(nameof(BufferSize), SocketConstants.DefaultBufferSize);
            RequestTimeout = conf.GetTimeSpan(nameof(RequestTimeout), TimeSpan.FromSeconds(10));
            Overloaded = conf.GetInt32(nameof(Overloaded));
        }
    }
}

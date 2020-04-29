using System;
using Framework.Configuration;

namespace ZyGames.Framework.Services.Options
{
    public class ConnectionListenerOptions
    {
        private SlioAddress insideAddress;
        private SlioAddress outsideAddress;

        public SlioAddress InsideAddress
        {
            get => insideAddress;
            set => insideAddress = value;
        }

        public SlioAddress OutsideAddress
        {
            get => outsideAddress ?? insideAddress;
            set => outsideAddress = value;
        }

        public int Backlog { get; set; }

        public int MaxConnections { get; set; }

        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public int Overloaded { get; set; }

        public virtual void CopyFrom(Config conf)
        {
            if (conf == null)
                throw new ArgumentNullException(nameof(conf));

            insideAddress = new SlioAddress(conf.GetString(nameof(InsideAddress)));
            if (conf.HasPath(nameof(OutsideAddress)))
            {
                outsideAddress = new SlioAddress(conf.GetString(nameof(OutsideAddress)));
            }

            Backlog = conf.GetInt32(nameof(Backlog));
            MaxConnections = conf.GetInt32(nameof(MaxConnections));
            RequestTimeout = conf.GetTimeSpan(nameof(RequestTimeout), TimeSpan.FromSeconds(10));
            Overloaded = conf.GetInt32(nameof(Overloaded));
        }
    }
}

using System.IO.Pipes;

namespace ZyGames.Framework.Remote.Networking
{
    public class BasicPipeBindingOptions
    {
        public string ServiceName = ".";

        public string PipeName;

        public int MaxAllowedServerInstances = NamedPipeServerStream.MaxAllowedServerInstances;
    }
}

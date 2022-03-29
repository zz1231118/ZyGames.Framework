using System;

namespace ZyGames.Framework.Services.Dashboard
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public sealed class NoProfilingAttribute : Attribute
    { }
}

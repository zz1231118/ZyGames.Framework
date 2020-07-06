using System;
using System.Threading.Tasks;

namespace ZyGames.Framework.Linq
{
    internal static class TaskExtentions
    {
        public static void WaitWithThrow(this Task task, TimeSpan timeout)
        {
            if (!task.Wait(timeout))
            {
                throw new TimeoutException($"Task.WaitWithThrow has timed out after {timeout}.");
            }
        }
    }
}

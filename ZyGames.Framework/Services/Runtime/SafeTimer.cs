using System;
using System.Threading;
using Framework.Log;
using ZyGames.Framework.Linq;

namespace ZyGames.Framework.Services.Runtime
{
    internal class SafeTimer : IDisposable
    {
        private readonly ILogger logger = Logger.GetLogger<SafeTimer>();
        private Timer timer;
        private TimerCallback callbackFunc;
        private TimeSpan dueTime;
        private TimeSpan timerFrequency;
        private bool timerStarted;
        private DateTime previousTickTime;
        private int totalNumTicks;

        internal SafeTimer(TimerCallback callback, object state)
        {
            Init(callback, state, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        internal SafeTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            Init(callback, state, dueTime, period);
            Start(dueTime, period);
        }

        public void Start(TimeSpan due, TimeSpan period)
        {
            if (timerStarted) 
                throw new InvalidOperationException(string.Format("Calling start on timer {0} is not allowed, since it was already created in a started mode with specified due.", typeof(SafeTimer).FullName));
            if (period == TimeSpan.Zero) 
                throw new ArgumentOutOfRangeException(nameof(period), period, "Cannot use TimeSpan.Zero for timer period");

            timerFrequency = period;
            dueTime = due;
            timerStarted = true;
            previousTickTime = DateTime.UtcNow;
            timer.Change(due, Timeout.InfiniteTimeSpan);
        }

        private void Init(TimerCallback callback, object state, TimeSpan due, TimeSpan period)
        {
            if (callback == null) 
                throw new ArgumentNullException("synCallback", "Cannot use null for both sync and asyncTask timer callbacks.");
            if (period == TimeSpan.Zero) 
                throw new ArgumentOutOfRangeException("period", period, "Cannot use TimeSpan.Zero for timer period");

            callbackFunc = callback;
            timerFrequency = period;
            dueTime = due;
            totalNumTicks = 0;

            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                timer = new Timer(HandleTimerCallback, state, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
            finally
            {
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeTimer();
            }
        }

        internal void DisposeTimer()
        {
            if (timer != null)
            {
                try
                {
                    var t = timer;
                    timer = null;
                    if (Logger.IsEnabled(Level.Debug))
                    {
                        logger.Debug("Disposing timer {0}", typeof(SafeTimer).FullName);
                    }

                    t.Dispose();
                }
                catch (Exception exc)
                {
                    logger.Warn("Ignored error disposing timer {0} error:{1}", typeof(SafeTimer).FullName, exc);
                }
            }
        }

        public bool CheckTimerFreeze(DateTime lastCheckTime, Func<string> callerName)
        {
            return CheckTimerDelay(previousTickTime, totalNumTicks, dueTime, timerFrequency, logger, () => string.Format("{0}.{1}", typeof(SafeTimer).FullName, callerName()), true);
        }

        public static bool CheckTimerDelay(DateTime previousTickTime, int totalNumTicks, TimeSpan dueTime, TimeSpan timerFrequency, ILogger logger, Func<string> getName, bool freezeCheck)
        {
            TimeSpan timeSinceLastTick = DateTime.UtcNow - previousTickTime;
            TimeSpan exceptedTimeToNexTick = totalNumTicks == 0 ? dueTime : timerFrequency;
            TimeSpan exceptedTimeWithSlack;
            if (exceptedTimeToNexTick >= TimeSpan.FromSeconds(6))
            {
                exceptedTimeWithSlack = exceptedTimeToNexTick + TimeSpan.FromSeconds(3);
            }
            else
            {
                exceptedTimeWithSlack = exceptedTimeToNexTick.Multiply(1.5);
            }
            if (timeSinceLastTick <= exceptedTimeWithSlack)
            {
                return true;
            }

            var errMsg = String.Format("{0}{1} did not fire on time. Last fired at {2}, {3} since previous fire, should have fired after {4}.",
                freezeCheck ? "Watchdog Freeze Alert: " : "-",
                getName == null ? "" : getName(),
                previousTickTime,
                timeSinceLastTick,
                exceptedTimeToNexTick);
            if (freezeCheck)
            {
                logger.Error(errMsg);
            }
            else
            {
                logger.Warn(errMsg);
            }
            return false;
        }

        private bool Change(TimeSpan newDueTime, TimeSpan period)
        {
            if (period == TimeSpan.Zero) throw new ArgumentOutOfRangeException("period", period, string.Format("Cannot use TimeSpan.Zero for timer {0} period", typeof(SafeTimer).FullName));

            if (timer == null) return false;

            timerFrequency = period;
            if (Logger.IsEnabled(Level.Debug))
            {
                logger.Debug("Changing timer {0} to dueTime={1} period={2}", typeof(SafeTimer).FullName, newDueTime, period);
            }

            try
            {
                return timer.Change(newDueTime, Timeout.InfiniteTimeSpan);
            }
            catch (Exception exc)
            {
                logger.Warn("Error changing timer period - timer {0} not changed. error:{1}", typeof(SafeTimer).FullName, exc);
                return false;
            }
        }

        private void HandleTimerCallback(object state)
        {
            if (timer != null)
            {
                try
                {
                    if (Logger.IsEnabled(Level.Trace))
                    {
                        logger.Trace("About to make sync timer callback for timer {0}", typeof(SafeTimer).FullName);
                    }

                    callbackFunc(state);
                    if (Logger.IsEnabled(Level.Trace))
                    {
                        logger.Trace("Completed sync timer callback for timer {0}", typeof(SafeTimer).FullName);
                    }
                }
                catch (Exception exc)
                {
                    logger.Warn("Ignored exception {0} during sync timer callback {1}. error:{2}", exc.Message, typeof(SafeTimer).FullName, exc);
                }
                finally
                {
                    previousTickTime = DateTime.UtcNow;
                    QueueNextTimerTick();
                }
            }
        }

        private void QueueNextTimerTick()
        {
            try
            {
                if (timer == null)
                {
                    return;
                }

                totalNumTicks++;
                if (Logger.IsEnabled(Level.Trace))
                {
                    logger.Trace("About to QueueNextTimerTick for timer {0}", typeof(SafeTimer).FullName);
                }
                if (timerFrequency == Timeout.InfiniteTimeSpan)
                {
                    DisposeTimer();
                    if (Logger.IsEnabled(Level.Trace)) logger.Trace("Timer {0} is now stopped and disposed", typeof(SafeTimer).FullName);
                }
                else
                {
                    timer.Change(timerFrequency, Timeout.InfiniteTimeSpan);
                    if (Logger.IsEnabled(Level.Trace)) logger.Trace("Queued next tick for timer {0} in {1}", typeof(SafeTimer).FullName, timerFrequency);
                }
            }
            catch (ObjectDisposedException ode)
            {
                logger.Warn("Timer {0} already disposed - will not queue next timer tick. error:{1}", typeof(SafeTimer).FullName, ode);
            }
            catch (Exception exc)
            {
                logger.Error("Error queueing next timer tick - WARNING: timer {0} is now stopped. error:{1}", typeof(SafeTimer).FullName, exc);
            }
        }
    }
}

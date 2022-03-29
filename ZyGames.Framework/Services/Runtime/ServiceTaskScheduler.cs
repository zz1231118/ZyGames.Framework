using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Framework.Log;

namespace ZyGames.Framework.Services.Runtime
{
    public class ServiceTaskScheduler : TaskScheduler
    {
        private readonly ILogger logger = Logger.GetLogger<ServiceTaskScheduler>();
        private readonly BlockingCollection<Task> tasks = new BlockingCollection<Task>();
        private readonly ConcurrentDictionary<int, Thread> threads = new ConcurrentDictionary<int, Thread>();
        private int minWorkerThread;
        private int maxWorkerThread;
        private int currentWorkerThread;
        private int completedWorkerThread;

        public ServiceTaskScheduler(int minWorkerThread, int maxWorkerThread)
        {
            if (minWorkerThread <= 0)
                throw new ArgumentOutOfRangeException(nameof(minWorkerThread));
            if (maxWorkerThread < minWorkerThread)
                throw new ArgumentOutOfRangeException(nameof(maxWorkerThread));

            this.minWorkerThread = minWorkerThread;
            this.maxWorkerThread = maxWorkerThread;
        }

        public ServiceTaskScheduler()
            : this(Environment.ProcessorCount, 32767)
        { }

        public int MinWorkerThread
        {
            get => minWorkerThread;
            set 
            {
                if (minWorkerThread <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                minWorkerThread = value;
            }
        }

        public int MaxWorkerThread
        {
            get => maxWorkerThread;
            set
            {
                if (maxWorkerThread < minWorkerThread)
                    throw new ArgumentOutOfRangeException(nameof(value));

                maxWorkerThread = value;
            }
        }

        public int AvailableWorkerThread => maxWorkerThread - currentWorkerThread;

        public int ThreadCount => threads.Count;

        public int WaitTaskCount => tasks.Count;

        private void Execute()
        {
            Task task;
            while (true)
            {
                task = tasks.Take();
                Interlocked.Increment(ref currentWorkerThread);

                try
                {
                    if (!TryExecuteTask(task))
                    {
                        logger.Warn("Execute: Incomplete base.TryExecuteTask for Task Id={0} with Status={1}", task.Id, task.Status);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Execute: Worker thread {0} caught an exception thrown from Execute: {1}", Thread.CurrentThread.ManagedThreadId, ex);
                }
                finally
                {
                    Interlocked.Decrement(ref currentWorkerThread);
                }
                if (completedWorkerThread > minWorkerThread)
                {
                    if (Interlocked.Decrement(ref completedWorkerThread) >= minWorkerThread)
                    {
                        threads.TryRemove(Thread.CurrentThread.ManagedThreadId, out _);
                        break;
                    }
                    else
                    {
                        Interlocked.Increment(ref completedWorkerThread);
                    }
                }
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return tasks;
        }

        protected override void QueueTask(Task task)
        {
            tasks.Add(task);
            if (currentWorkerThread >= completedWorkerThread && completedWorkerThread < maxWorkerThread)
            {
                if (Interlocked.Increment(ref completedWorkerThread) <= maxWorkerThread)
                {
                    var thread = new Thread(new ThreadStart(Execute));
                    thread.IsBackground = true;
                    threads[thread.ManagedThreadId] = thread;
                    thread.Start();
                }
                else
                {
                    Interlocked.Decrement(ref completedWorkerThread);
                }
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            var canExecuteInline = true;
            if (taskWasPreviouslyQueued)
            {
                canExecuteInline = TryDequeue(task);
            }
            if (!canExecuteInline)
            {
                if (Logger.IsEnabled(Level.Trace)) logger.Trace("TryExecuteTaskInline Task Id={0} Status={1} Execute=No", task.Id, task.Status);
                return false;
            }
            var done = TryExecuteTask(task);
            if (!done)
            {
                logger.Warn("TryExecuteTaskInline: Incomplete base.TryExecuteTask for Task Id={0} with Status={1}", task.Id, task.Status);
            }

            return done;
        }
    }
}

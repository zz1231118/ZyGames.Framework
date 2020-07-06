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
        private int blockingWorkerThread;
        private int completedWorkerThread;

        public ServiceTaskScheduler(int blockingWorkerThread)
        {
            if (blockingWorkerThread <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockingWorkerThread));

            this.blockingWorkerThread = blockingWorkerThread;
        }

        public ServiceTaskScheduler()
            : this(Environment.ProcessorCount)
        { }

        public int BlockingWorkerThread
        {
            get => blockingWorkerThread;
            set 
            {
                if (blockingWorkerThread <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                blockingWorkerThread = value;
            }
        }

        public int CompletedWorkerThread => completedWorkerThread;

        private void Execute()
        {
            Task task;
            while (true)
            {
                task = tasks.Take();

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
                if (completedWorkerThread > blockingWorkerThread)
                {
                    if (Interlocked.Decrement(ref completedWorkerThread) >= blockingWorkerThread)
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
            if (tasks.Count > 0 && completedWorkerThread < blockingWorkerThread)
            {
                if (Interlocked.Increment(ref completedWorkerThread) <= blockingWorkerThread)
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
                if (logger.IsEnabled(LogLevel.Trace)) logger.Trace("TryExecuteTaskInline Task Id={0} Status={1} Execute=No", task.Id, task.Status);
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

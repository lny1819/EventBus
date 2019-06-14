using Microsoft.Extensions.Logging;
using ML.Fulturetrade.EventBus.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ML.Soa.Sp
{
    public class LimitedTaskSchedulerByThreadPool : TaskScheduler
    {
        private readonly ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();
        private readonly int _maxDegreeOfParallelism;
        private int _delegatesQueuedOrRunning = 0;
        private ILogger<LimitedTaskSchedulerByThreadPool> _logger;
        private long _queueLength = 0;
        private readonly int _maxQueueLength = 0;
        /// <summary>
        /// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the
        /// specified degree of parallelism.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism provided by this scheduler.</param>
        public LimitedTaskSchedulerByThreadPool(int maxDegreeOfParallelism, IQpsCounter counter, ILogger<LimitedTaskSchedulerByThreadPool> logger, bool pubmode)
        {
            _logger = logger;
            Counter = counter ?? throw new ArgumentNullException(nameof(IQpsCounter));
            if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _maxQueueLength = _maxDegreeOfParallelism * 1000;
            if (pubmode)
            {
                _maxQueueLength = _maxDegreeOfParallelism * 7000;
            }
        }

        /// <summary>Queues a task to the scheduler.</summary>
        /// <param name="task">The task to be queued.</param>
        protected sealed override void QueueTask(Task task)
        {
            _tasks.Enqueue(task);
            var x = Interlocked.Increment(ref _queueLength);
            var i = Interlocked.Increment(ref _delegatesQueuedOrRunning);
            if (i <= _maxDegreeOfParallelism) NotifyThreadPoolOfPendingWork();
            else Interlocked.Decrement(ref _delegatesQueuedOrRunning);
            if (x > _maxQueueLength)
            {
                _logger.LogWarning($"LimitedTaskSchedulerByThreadPool TaskQueue length is lagrger then {_maxQueueLength.ToString()}, now is pending....");
                Thread.Sleep(3);
            }
        }
        public IQpsCounter Counter { get; }

        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                for (; ; )
                {
                    if (!_tasks.TryDequeue(out Task item))
                    {
                        Interlocked.Decrement(ref _delegatesQueuedOrRunning);
                        break;
                    }
                    base.TryExecuteTask(item);
                    Interlocked.Decrement(ref _queueLength);
                }
            }, null);
        }

        /// <summary>Attempts to execute the specified task on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued"></param>
        /// <returns>Whether the task could be executed on the current thread.</returns>
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            var i = Interlocked.CompareExchange(ref _delegatesQueuedOrRunning, 0, 0);
            // If this thread isn't already processing a task, we don't support inlining
            if (i == 0) return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued) TryDequeue(task);

            // Try to run the task.
            return base.TryExecuteTask(task);
        }

        /// <summary>Attempts to remove a previously scheduled task from the scheduler.</summary>
        /// <param name="task">The task to be removed.</param>
        /// <returns>Whether the task could be found and removed.</returns>
        protected sealed override bool TryDequeue(Task task)
        {
            throw new NotSupportedException();
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public sealed override int MaximumConcurrencyLevel
        {
            get
            {
                return _maxDegreeOfParallelism;
            }
        }

        /// <summary>Gets an enumerable of the tasks currently scheduled on this scheduler.</summary>
        /// <returns>An enumerable of the tasks currently scheduled.</returns>
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks.ToArray();
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }
    }
}

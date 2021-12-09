using System.Collections.Generic;
using System.Diagnostics;
using System;
using UnityEngine;

namespace TreeSharpPlus
{
    /// <summary>
    ///    Loop executing the node argument, and return RUNNING when the time has not passed
    ///    return SUCCESS when the time has passed
    /// </summary>
    public class LoopUntilTimeOut : Decorator
    {
        public int Iterations { get; set; }

        protected Stopwatch stopwatch;
        protected long waitMax;

        /// <summary>
        ///    Initializes with the wait period
        /// </summary>
        /// <param name="waitMax">The time (in milliseconds) for which to 
        /// wait</param>
        public LoopUntilTimeOut(Node child, Val<long> waitMax)
            : base(child)
        {
            this.waitMax = waitMax.Value;
            this.stopwatch = new Stopwatch();
            this.Iterations = -1;

        }

        /// <summary>
        ///    Resets the wait timer
        /// </summary>
        /// <param name="context"></param>
        public override void Start()
        {
            base.Start();
            this.stopwatch.Reset();
            this.stopwatch.Start();
        }

        public override void Stop()
        {
            base.Stop();
            this.stopwatch.Stop();
        }

        public override IEnumerable<RunStatus> Execute()
        {
            // Keep track of the running iterations
            int curIter = 0;

            this.DecoratedChild.Start();

            while (true)
            {

                RunStatus result;
                if ((result = this.TickNode(this.DecoratedChild)) == RunStatus.Running)
                    yield return RunStatus.Running;

                // If the child failed. restart in the next round
                if (result == RunStatus.Failure || result == RunStatus.Success)
                {
                    this.DecoratedChild.Stop();  // child has stopped
                    this.DecoratedChild.Start();
                    yield return RunStatus.Running;
                }

                // Check timer to see if we're done
                curIter++;
                if (this.stopwatch.ElapsedMilliseconds >= this.waitMax)
                {

                    this.DecoratedChild.Stop();  // child has stopped
                    yield return RunStatus.Success;
                    yield break;
                }
                // Take one tick to prevent infinite loops
                yield return RunStatus.Running;
            }
        }

    }
}


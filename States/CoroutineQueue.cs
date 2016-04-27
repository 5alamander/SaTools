using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.cstools.Plugins {

    public class CoroutineQueue {

        private readonly uint maxNum;

        private uint currentNum;

        /// <summary>
        ///     Delegate to start coroutines with
        /// </summary>
        private readonly Func<IEnumerator, Coroutine> coroutineStarter;

        private readonly Queue<IEnumerator> queue;

        /// <summary>
        ///     Create the queue, initially with no coroutines
        /// </summary>
        public CoroutineQueue (Func<IEnumerator, Coroutine> coroutineStarter, uint maxNum) {
            if (maxNum == 0) {
                throw new ArgumentException("Must be at least one", "maxNum");
            }
            this.maxNum = maxNum;
            this.coroutineStarter = coroutineStarter;
            queue = new Queue<IEnumerator>();
        }

        /// <summary>
        ///     create more friendly
        /// </summary>
        public CoroutineQueue (MonoBehaviour behaviour, uint maxNum)
            : this(behaviour.StartCoroutine, maxNum) {
        }

        /// <summary>
        ///     call CoroutineQueue#Run(coroutine...)
        /// </summary>
        public void Run (IEnumerator coroutine) {
            if (currentNum < maxNum) {
                var runner = CoroutineRunner(coroutine);
                coroutineStarter(runner);
            }
            else {
                queue.Enqueue(coroutine);
            }
        }

        /// <summary>
        ///     start run coroutine or enqueue it
        /// </summary>
        private IEnumerator CoroutineRunner (IEnumerator coroutine) {
            currentNum++;
            while (coroutine.MoveNext()) {
                yield return coroutine.Current;
            }
            currentNum--;
            if (queue.Count > 0) {
                var next = queue.Dequeue();
                Run(next);
            }
        }
    }

}
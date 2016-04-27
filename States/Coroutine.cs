using System;
using System.Collections;
using UnityEngine;

namespace Assets.cstools.Plugins {

    // the exception
    public class CoroutineCancelledException : Exception {}

    // notice in MonoBehavior scope, you must use this.StartCoroutine<sometype>(...)
    public static class MonoBehaviourExt {
        public static Coroutine<T> StartCoroutine<T>(
            this MonoBehaviour obj, IEnumerator coroutine) {
            var coroutineObject = new Coroutine<T>();
            coroutineObject.coroutine = obj.StartCoroutine(
                coroutineObject.InternalRoutine(coroutine));
            return coroutineObject;
        }
    }

    /// <summary>
    ///     Coroutine-Object: an extension for debug, return value, and cancel
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Coroutine<T> {
        public T Value {
            get {
                if (exception != null) {
                    throw exception;
                }
                return returnVal;
            }
        }

        public void Cancel() {
            isCancelled = true;
        }

        private bool isCancelled;
        private T returnVal;
        private Exception exception;
        public Coroutine coroutine;

        public IEnumerator InternalRoutine(IEnumerator theCoroutine) {
            while (true) {
                if (isCancelled) {
                    exception = new CoroutineCancelledException();
                    yield break;
                }
                try {
                    if (!theCoroutine.MoveNext()) {
                        yield break;
                    }
                }
                catch (Exception e) {
                    exception = e;
                    yield break;
                }
                var yielded = theCoroutine.Current;
                if (yielded != null && yielded.GetType() == typeof(T)) {
                    returnVal = (T)yielded;
                    yield break;
                }
                yield return theCoroutine.Current;
            }
        }
    }
}

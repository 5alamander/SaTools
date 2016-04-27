using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.cstools.Plugins {

    /// <summary>
    ///     passitive state mechine<br />
    ///     <c>Notice</c> this is based on Frame update<br />
    ///     ----trans state-------invoke action-------trans end------ <br />
    ///     |update,lateUpdate||update,lateUpdate||update,lateUpdate| <br />
    /// </summary>
    public class StateMechine : MonoBehaviour {
        
        public static readonly object InitState = new Object();

        // attach to a gameObject
        public static StateMechine get (GameObject go) {
            return go.GetComponent<StateMechine>()
                   ?? go.AddComponent<StateMechine>();
        }

        public int framIdentife { get; private set; }
        public object lastState { get; private set; }
        public object currentState { get; private set; }
        private readonly Queue<object> stateQueue = new Queue<object>();
        private object enQueuedState = InitState; // use a default

        private bool isTransForming;

        protected void LateUpdate () {
            // reset the type when every thing is done
            if (isTransForming) {
                // ins to use framIdentife < time.framCount - 1
                if (framIdentife == Time.frameCount - 2)
                    isTransForming = false;
            }
            else {
                // pop form the queue
                if (stateQueue.Count > 0) {
                    trans_(stateQueue.Dequeue());
                }
            }
        }

        // will trigger onTrans
        public void setState (object state) {
            enQueuedState = state;
            currentState = state;
        }

        // will not trigger onTrans
        public void initState (object state) {
            lastState = InitState;
            setState(state);
        }

        /// <summary>
        ///     do not use immediately when trans state, or on state trans
        /// </summary>
        /// <param name="cState">current state</param>
        /// <param name="target">target state</param>
        public bool trans (object cState, object target) {
            if (isTransForming || stateQueue.Count > 0) {
                // when busy, add to queue
                if (!enQueuedState.Equals(cState)) return false;
                stateQueue.Enqueue(target);
                enQueuedState = target;
                return true;
            }
            if (!currentState.Equals(cState)) return false;
            // set state and set this state is transforming
            trans_(target);
            enQueuedState = target;
            return true;
        }

        // may add a hook here
        protected virtual void trans_ (object target) {
            lastState = currentState;
            currentState = target;
            framIdentife = Time.frameCount;
            isTransForming = true;
        }

        /// <summary>
        ///     use this in update, <br />
        ///     return true when state transform is (ls, cs) <br />
        ///     <c>notice</c> this is one frame after the state trans
        /// </summary>
        public bool onTrans (object lState, object cState) {
            return framIdentife == Time.frameCount - 1
                   && lastState.Equals(lState)
                   && currentState.Equals(cState);
        }

        /// <summary>
        ///     return true when state is current state
        /// </summary>
        public bool onState (object state) {
            return currentState.Equals(state);
        }

        /// <summary>
        ///     not equals the onTrans function, but for sub-class
        /// </summary>
        public bool onState (object lState, object cState) {
            return lastState.Equals(lState) && currentState.Equals(cState);
        }
    }

}
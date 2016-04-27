using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Assets.cstools.Plugins {

    public class OnStateAttribute : Attribute {

        public object State1 { get; private set; }
        public object State2 { get; private set; }

        public StateStation.StateListenerAction TheDelegate { get; set; }

        public OnStateAttribute (object state)
            : this(state, null) { }

        public OnStateAttribute (object fromState, object toState) {
            State1 = fromState;
            State2 = toState;
        }

        public bool IsTrans { get { return State2 != null; } }
    }

    public class StateStation : StateMechine {

        // only apply on void ..()
        public delegate void StateListenerAction ();

        // the scope
        public readonly BindingFlags ListenerScopeBindingFlags =
            BindingFlags.Instance | BindingFlags.Public;
        
        // attach to a gameObject
        public static new StateStation get (GameObject go) {
            return go.GetComponent<StateStation>()
                   ?? go.AddComponent<StateStation>();
        }

        // on trans list
        private readonly List<OnStateAttribute> onTransList = new List<OnStateAttribute>(); 
        // on stay list
        private readonly List<OnStateAttribute> onStayList = new List<OnStateAttribute>();

        public void register (object listener) {
            traversInsMethodsWith<OnStateAttribute>(listener, (methodInfo, attr) => {
                var theDelegate = (StateListenerAction) Delegate.CreateDelegate(
                    typeof (StateListenerAction), listener, methodInfo, false);
                if (theDelegate == null) return;
                attr.TheDelegate = theDelegate; // todo set dirty
                if (attr.IsTrans) onTransList.Add(attr);
                else onStayList.Add(attr);
                // *** debug ***
//                Debug.Log(methodInfo.Name + " " + attr.State1 + attr.State2);
//                Debug.Log(methodInfo.ToString());
            }, ListenerScopeBindingFlags);
        }

        // move to Gs facade
        public static void traversInsMethodsWith<T> (object obj, Action<MethodInfo, T> action,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
            where T : Attribute {
            var theObjType = obj.GetType();
            foreach (var methodInfo in theObjType.GetMethods(bindingFlags)) {
                var attr = (T) Attribute.GetCustomAttribute(methodInfo, typeof (T));
                if (attr == null) continue;
                action(methodInfo, attr);
            }
        }

        // a new update with checking
        protected new void LateUpdate () {
            base.LateUpdate();
            foreach (var attr in onStayList) { // check, then execute
                if (onState(attr.State1)) {
                    attr.TheDelegate();
                }
            }
        }

        // add a hook on trans_
        protected override void trans_ (object target) {
            base.trans_(target);
            foreach (var attr in onTransList) { // check, then execute 
                if (onState(attr.State1, attr.State2)) {
                    attr.TheDelegate();
                }
            }
        }
    }

}
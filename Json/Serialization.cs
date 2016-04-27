using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

namespace Assets.cstools.Plugins {

    /// <summary>
    ///     dictionary only accept keys in string
    /// </summary>
    public static class JsonUtilityEx {

        public static bool isGenericOf (string typeStr, object obj) {
            return isGenericOf(typeStr, obj.GetType());
        }

        public static bool isGenericOf (string typeStr, Type type) {
            if (!type.IsGenericType) return false;
            // for example "List`1"
            var pattern = @"^" + typeStr + @"`.+";
            return Regex.Match(type.Name, pattern).Success;
        }

        // serialization<> ins
        private static Type listSerializationType (Type templateType) {
            return typeof (Serialization<>).MakeGenericType(
                templateType.GetGenericArguments());
        }

        private static Type dictSerializationType (Type templateType) {
            return typeof (Serialization<,>).MakeGenericType(
                templateType.GetGenericArguments());
        }

        // learn to create Serialization<> in code ins, may impossible
        private static Type makeGenericLike<T> (Type templateType) {
            return typeof (T).MakeGenericType(
                templateType.GetGenericArguments());
        }

        /// <summary>
        ///     to-json and wraped with an object,
        ///     which has a constructor for in argument;
        /// </summary>
        private static string toJsonWithWrapType (object obj,
            bool prettyPrint, Type serializationType) {
            var serialization = Activator.CreateInstance(
                serializationType, obj);
            return JsonUtility.ToJson(serialization, prettyPrint);
        }

        public static string ToJson (object obj, bool prettyPrint = false) {
            if (isGenericOf("List", obj)) {
                var serializationType = listSerializationType(obj.GetType());
                return toJsonWithWrapType(obj, prettyPrint, serializationType);
            }
            if (isGenericOf("Dictionary", obj)) {
                Assert.AreEqual(obj.GetType().GetGenericArguments()[0], typeof(string),
                    "jsonUtilityEx Serialization only support dict<string, T>");
                var serializationType = dictSerializationType(obj.GetType());
                return toJsonWithWrapType(obj, prettyPrint, serializationType);
            }
            return JsonUtility.ToJson(obj, prettyPrint);
        }

        public static T FromJson<T> (string str) {
            return FromJson(str, typeof (T)) is T 
                ? (T) FromJson(str, typeof (T)) 
                : default(T);
        }

        public static string formateListJson (string str) {
            return Regex.Match(str, @"^\s*\[[\s\S]*\]\s*$").Success 
                ? "{\"target\":\n" + str + "\n}" 
                : str;
        }

        public static string formateListOutput (string output) {
            var m = Regex.Match(output, @"(\[[\s\S]*\])");
            return m.Success ? m.Groups[0].Value : "";
        }

        public static string cutTheListWrapper (string str) {
            throw new NotImplementedException();
        }

        public static object FromJson (string str, Type type) {
            if (isGenericOf("List", type)) {
                var serializationType = listSerializationType(type);
                var genericMethod = serializationType.GetMethod("ToList");
                var serialization = JsonUtility.FromJson(
                    formateListJson(str), serializationType);
                return genericMethod.Invoke(serialization, null);
            }
            if (isGenericOf("Dictionary", type)) {
                Assert.AreEqual(type.GetGenericArguments()[0], typeof(string),
                    "jsonUtilityEx Serialization only support dict<string, T>");
                var serializationType = dictSerializationType(type);
                var genericMethod = serializationType.GetMethod("ToDictionary");
                var serialization = JsonUtility.FromJson(str, serializationType);
                return genericMethod.Invoke(serialization, null);
            }
            return JsonUtility.FromJson(str, type);
        }

        /// <summary>
        ///     it seems not impossible to create List
        /// </summary>
        public static void FromJsonOverwrite (string str, object obj) {
            if (isGenericOf("List", obj)) {
                var baseList = (IList) obj;
                var newList = (IList) FromJson(str, obj.GetType());
                // replace list
                baseList.Clear();
                foreach (var value in newList) {
                    baseList.Add(value);
                }
            }
            else if (isGenericOf("Dictionary", obj)) {
                var baseDict = (IDictionary) obj;
                var newDict = (IDictionary) FromJson(str, obj.GetType());
                // merge dict
                foreach (DictionaryEntry entry in newDict) {
                    baseDict[entry.Key] = entry.Value;
//                    baseDict.Add(entry.Key, entry.Value);
                }
            }
            else {
                JsonUtility.FromJsonOverwrite(str, obj);
            }
        }

        static IList createList(params Type[] myType) {
            var genericListType = typeof(List<>).MakeGenericType(myType);
            return (IList)Activator.CreateInstance(genericListType);
        }

        public static List<T> loadList<T> (string jsonAsset) {
            var textAsset = Resources.Load(jsonAsset) as TextAsset;
            if (textAsset == null) {
                throw new Exception(jsonAsset + " load error");
            }
            var str = textAsset.text;
            return jsonStrToList<T>(str);
        }

        public static List<T> jsonStrToList<T> (string str) {
            return FromJson<List<T>>(str);
        }

        public static Dictionary<string, T> loadDict<T> (
            string jsonAsset, Func<T, string> keyFunc) {
            var list = loadList<T>(jsonAsset);
            var dict = new Dictionary<string, T>();
            Assert.IsNotNull(list);
            foreach (var element in list) {
                dict.Add(keyFunc(element), element);
            }
            return dict;
        }

    }

    // serialize and deserialize with list and dict
    // http://kou-yeung.hatenablog.com/entry/2015/12/31/014611

    // List<T>
    [Serializable]
    class Serialization<T> {

        [SerializeField]
        List<T> target; // can not use readonly

        public List<T> ToList() { return target; }

//        public object toList() { return target; }

        public Serialization(List<T> target) {
            this.target = target;
        }
    }

    /// <summary>
    ///     Dictionary[TKey, TValue], 
    ///     ins class with generic can't be serializeField
    ///     may use hash instead
    /// </summary>
    [Serializable]
    class Serialization<TKey, TValue> : ISerializationCallbackReceiver {

        [SerializeField]
        List<TKey> keys;

        [SerializeField]
        List<TValue> values;

        Dictionary<TKey, TValue> target;

        public Dictionary<TKey, TValue> ToDictionary() { return target; }

        public Serialization(Dictionary<TKey, TValue> target) {
            this.target = target;
        }

        public void OnBeforeSerialize() {
            keys = new List<TKey>(target.Keys);
            values = new List<TValue>(target.Values);
        }

        public void OnAfterDeserialize() {
            var count = Math.Min(keys.Count, values.Count);
            target = new Dictionary<TKey, TValue>(count);
            for (var i = 0; i < count; ++i) {
                target.Add(keys[i], values[i]);
            }
        }
    }

    /// <summary>
    ///     just support for [string, string]
    /// </summary>
    [Serializable]
    public class SerializationHash : ISerializationCallbackReceiver {

        [SerializeField]
        List<string> keys;

        [SerializeField]
        List<string> values;

        Hashtable target;

        public Hashtable ToDictionary() { return target; }

        public SerializationHash(Hashtable target) {
            this.target = target;
        }

        public void OnBeforeSerialize() {
            // ins

            // this use linq
//            keys = target.Keys.Cast<string>().ToList();
//            values = target.Keys.Cast<string>().ToList();
//            keys = new List<TKey>(target.Keys);
//            values = new List<TValue>(target.Values);
        }

        public void OnAfterDeserialize() {
            var count = Math.Min(keys.Count, values.Count);
            target = new Hashtable(count);
            for (var i = 0; i < count; ++i) {
                target.Add(keys[i], values[i]);
            }
        }
    }

    // BitArray
    [Serializable]
    public class SerializationBitArray : ISerializationCallbackReceiver {

        [SerializeField]
        string flags;

        BitArray target;

        public BitArray ToBitArray() { return target; }

        public SerializationBitArray(BitArray target) {
            this.target = target;
        }

        public void OnBeforeSerialize() {
            var ss = new System.Text.StringBuilder(target.Length);
            for (var i = 0; i < target.Length; ++i) {
                ss.Insert(0, target[i] ? '1' : '0');
            }
            flags = ss.ToString();
        }

        public void OnAfterDeserialize() {
            target = new BitArray(flags.Length);
            for (var i = 0; i < flags.Length; ++i) {
                target.Set(flags.Length - i - 1, flags[i] == '1');
            }
        }
    }

}
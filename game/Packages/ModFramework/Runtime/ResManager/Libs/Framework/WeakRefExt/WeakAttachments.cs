using System;

namespace WeakAttachments
{
    internal class WeakReferenceTracker
    {
        public WeakReferenceEx Target;
        public WeakReferenceTracker(WeakReferenceEx target)
        {
            Target = target;
        }

        ~WeakReferenceTracker()
        {
            if (Target != null && Target.Target != null)
            {
                GC.ReRegisterForFinalize(this);
            }
            else
            {
                WeakAttachmentsManager.ClearAttachments(Target);
            }
        }
    }

    internal sealed class WeakReferenceEx
    {
        private WeakReference _WeakRef;
        private WeakReference _Tracker;

        public object Target
        {
            get
            {
                if (_WeakRef != null)
                {
                    try
                    {
                        if (_WeakRef.IsAlive)
                        {
                            return _WeakRef.Target;
                        }
                    }
                    catch { }
                }
                return null;
            }
            set
            {
                _WeakRef = new WeakReference(value);
            }
        }

        public WeakReferenceEx()
        {
            _Tracker = new WeakReference(new WeakReferenceTracker(this));
        }
        public WeakReferenceEx(object target)
            : this()
        {
            Target = target;
        }

        private int _CachedHash;
        public override int GetHashCode()
        {
            var tar = Target;
            if (tar != null)
            {
                return _CachedHash = tar.GetHashCode();
            }
            return _CachedHash;
        }

        public override bool Equals(object obj)
        {
            if (obj is WeakReferenceEx)
            {
                var wr = obj as WeakReferenceEx;
                return object.ReferenceEquals(this, wr);
            }
            return object.ReferenceEquals(obj, Target);
        }
    }

    internal static class WeakAttachmentsManager
    {
        internal class WeakAttachmentsInfo
        {
            public WeakReferenceEx Parent;
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
            private System.Collections.Concurrent.ConcurrentDictionary<string, object> Attachments = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();

            public void SetAttachment(string name, object attach)
            {
                if (name == null)
                {
                    Attachments.Clear();
                }
                else if (attach == null)
                {
                    Attachments.TryRemove(name, out attach);
                }
                else
                {
                    Attachments[name] = attach;
                }
            }
            public object GetAttachment(string name)
            {
                object attach;
                if (Attachments.TryGetValue(name, out attach))
                {
                    return attach;
                }
                return null;
            }
#else
            private System.Collections.Generic.Dictionary<string, object> Attachments = new System.Collections.Generic.Dictionary<string, object>();

            public void SetAttachment(string name, object attach)
            {
                lock (Attachments)
                {
                    if (name == null)
                    {
                        Attachments.Clear();
                    }
                    else if (attach == null)
                    {
                        Attachments.Remove(name);
                    }
                    else
                    {
                        Attachments[name] = attach;
                    }
                }
            }
            public object GetAttachment(string name)
            {
                lock (Attachments)
                {
                    object attach;
                    if (Attachments.TryGetValue(name, out attach))
                    {
                        return attach;
                    }
                }
                return null;
            }
#endif
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        internal static System.Collections.Concurrent.ConcurrentDictionary<object, WeakAttachmentsInfo> Attachments = new System.Collections.Concurrent.ConcurrentDictionary<object, WeakAttachmentsInfo>();

        internal static void ClearAttachments(object parent)
        {
            WeakAttachmentsInfo info;
            Attachments.TryRemove(parent, out info);
        }
        internal static void ClearAttachments(WeakReferenceEx parent)
        {
            WeakAttachmentsInfo info;
            Attachments.TryRemove(parent, out info);
        }
        internal static WeakAttachmentsInfo GetAttachments(object parent)
        {
            if (parent == null)
            {
                return null;
            }
            WeakAttachmentsInfo info;
            if (Attachments.TryGetValue(parent, out info))
            {
                return info;
            }
            return null;
        }
        internal static WeakAttachmentsInfo GetOrCreateAttachments(object parent)
        {
            if (parent == null)
            {
                return null;
            }
            WeakAttachmentsInfo info;
            if (Attachments.TryGetValue(parent, out info))
            {
                return info;
            }
            info = new WeakAttachmentsInfo();
            if (parent is WeakReferenceEx)
            {
                info.Parent = parent as WeakReferenceEx;
            }
            else
            {
                info.Parent = new WeakReferenceEx(parent);
            }
            return Attachments.GetOrAdd(info.Parent, info);
        }
        private static WeakAttachmentsInfo CreateAttachmentsInfo(object parent)
        {
            var info = new WeakAttachmentsInfo();
            if (parent is WeakReferenceEx)
            {
                info.Parent = parent as WeakReferenceEx;
            }
            else
            {
                info.Parent = new WeakReferenceEx(parent);
            }
            return info;
        }
#else
        internal static System.Collections.Generic.Dictionary<object, WeakAttachmentsInfo> Attachments = new System.Collections.Generic.Dictionary<object, WeakAttachmentsInfo>();

        internal static void ClearAttachments(object parent)
        {
            lock (Attachments)
            {
                Attachments.Remove(parent);
            }
        }
        internal static void ClearAttachments(WeakReferenceEx parent)
        {
            lock (Attachments)
            {
                Attachments.Remove(parent);
            }
        }
        internal static WeakAttachmentsInfo GetAttachments(object parent)
        {
            if (parent == null)
            {
                return null;
            }
            lock (Attachments)
            {
                WeakAttachmentsInfo info;
                if (Attachments.TryGetValue(parent, out info))
                {
                    return info;
                }
                return null;
            }
        }
        internal static WeakAttachmentsInfo GetOrCreateAttachments(object parent)
        {
            if (parent == null)
            {
                return null;
            }
            lock (Attachments)
            {
                WeakAttachmentsInfo info;
                if (Attachments.TryGetValue(parent, out info))
                {
                    return info;
                }
                info = new WeakAttachmentsInfo();
                if (parent is WeakReferenceEx)
                {
                    info.Parent = parent as WeakReferenceEx;
                }
                else
                {
                    info.Parent = new WeakReferenceEx(parent);
                }
                Attachments[info.Parent] = info;
                return info;
            }
        }
#endif
    }

    public static class WeakAttachmentsExtensions
    {
        public static void SetAttachment(this object parent, string name, object attach)
        {
            if (name == null)
            {
                ClearAttachments(parent);
            }
            else if (attach == null)
            {
                RemoveAttachment(parent, name);
            }
            else
            {
                WeakAttachmentsManager.GetOrCreateAttachments(parent).SetAttachment(name, attach);
            }
        }
        public static void RemoveAttachment(this object parent, string name)
        {
            var attchments = WeakAttachmentsManager.GetAttachments(parent);
            if (attchments != null)
            {
                attchments.SetAttachment(name, null);
            }
        }
        public static void ClearAttachments(this object parent)
        {
            WeakAttachmentsManager.ClearAttachments(parent);
        }
        public static object GetAttachment(this object parent, string name)
        {
            var attchments = WeakAttachmentsManager.GetAttachments(parent);
            if (attchments != null)
            {
                return attchments.GetAttachment(name);
            }
            return null;
        }

        public static void SetAttachment<T>(this object parent, T attach)
        {
            SetAttachment(parent, typeof(T).Name, attach);
        }
        public static void RemoveAttachment<T>(this object parent)
        {
            RemoveAttachment(parent, typeof(T).Name);
        }
        public static T GetAttachment<T>(this object parent)
        {
            var attach = GetAttachment(parent, typeof(T).Name);
            if (attach is T)
            {
                return (T)attach;
            }
            else
            {
                return default(T);
            }
        }
    }
}
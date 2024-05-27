#if UNITY_EDITOR
using System.Threading;
using System.Collections.Generic;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        public class MessageCenter
        {
            public static MessageCenter instance { get; private set; }

            private Mutex _mutex = new Mutex();
            private List<System.Action> _messages = new List<System.Action>();

            public MessageCenter()
            {
                instance = this;
            }

            public void Release()
            {
                _mutex.WaitOne();
                _messages.Clear();
                _mutex.ReleaseMutex();
                instance = null;
            }

            public void PostMessage(System.Action action)
            {
                _mutex.WaitOne();
                _messages.Add(action);
                _mutex.ReleaseMutex();
            }

            public void Update()
            {
                _mutex.WaitOne();
                for (int i = 0; i < _messages.Count; ++i)
                {
                    if (_messages[i] != null)
                    {
                        _messages[i].Invoke();
                    }
                }
                _messages.Clear();
                _mutex.ReleaseMutex();
            }
        }
    }
}

#endif
#if UNITY_EDITOR
namespace Pico.AvatarAssetPreview
{
    public class ALogItemPool
    {
        public const int MAX_POOL_SIZE = 100;
        
        private object _poolSync;
        private int _poolSize;
        
        public ALogItem Pool;

        public ALogItemPool()
        {
            _poolSync = new object();
        }

        public ALogItem Obtain()
        {
            lock (_poolSync)
            {
                if (Pool != null)
                {
                    var m = Pool;
                    Pool = m.next;
                    m.next = null;
                    _poolSize--;

                    return m;
                }
            }

            return new ALogItem();
        }

        public void Recycle(ALogItem item)
        {
            item.Reset();
            lock (_poolSync)
            {
                if (_poolSize < MAX_POOL_SIZE)
                {
                    item.next = Pool;
                    Pool = item;
                    _poolSize++;
                }
            }
        }
    }
}
#endif
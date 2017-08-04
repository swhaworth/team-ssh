using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TeamSSHLibrary.Tunnelling
{
    public class BaseTunnelData
    {
        #region Fields

        protected readonly ConcurrentQueue<byte> _dataQueue = new ConcurrentQueue<byte>();

        #endregion

        #region Ctors

        public BaseTunnelData()
        {
            this.Added = new AutoResetEvent(false);
        }

        #endregion

        #region Properties

        public WaitHandle Added { get; private set; }

        #endregion        

        #region Public Methods

        public void Add(byte data)
        {
            _dataQueue.Enqueue(data);
            ((AutoResetEvent)this.Added)?.Set();
        }

        public void AddRange(IEnumerable<byte> data)
        {
            foreach (var item in data)
            {
                _dataQueue.Enqueue(item);
            }
            ((AutoResetEvent)this.Added)?.Set();
        }

        public bool Take(out byte data)
        {
            return _dataQueue.TryDequeue(out data);
        }

        public IEnumerable<byte> Take(int count)
        {
            var data = new List<byte>();
            for (var c = 0; c < count; ++c)
            {
                if (_dataQueue.TryDequeue(out var item))
                {
                    data.Add(item);
                }
            }
            return data;
        }

        #endregion
    }
}

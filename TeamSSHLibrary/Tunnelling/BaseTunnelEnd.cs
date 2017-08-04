using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TeamSSHLibrary.Tunnelling
{
    public abstract class BaseTunnelEnd
    {
        #region Fields

        protected CancellationTokenSource _close;

        #endregion

        #region Ctors

        protected BaseTunnelEnd(ILogger logger, string name, CancellationToken cancel)
        {
            _close = new CancellationTokenSource();
            this.Cancel = cancel;
            this.Logger = logger;
            this.Name = name;
        }

        #endregion

        #region Properties

        public CancellationToken Cancel { get; }
        public BaseTunnelData Incoming { get; private set; }
        public ILogger Logger { get; }
        public string Name { get; }
        public BaseTunnelEnd OtherEnd { get; private set; }
        public BaseTunnelData Outgoing { get; private set; }

        #endregion

        #region Public Methods

        public virtual void Close()
        {
            _close.Cancel();
        }

        public void Connect(BaseTunnelEnd otherEnd)
        {
            this.Incoming = otherEnd.Outgoing = new BaseTunnelData();
            this.Outgoing = otherEnd.Incoming = new BaseTunnelData();
            this.OtherEnd = otherEnd;
            otherEnd.OtherEnd = this;
        }

        public abstract Task Start();

        #endregion
    }
}

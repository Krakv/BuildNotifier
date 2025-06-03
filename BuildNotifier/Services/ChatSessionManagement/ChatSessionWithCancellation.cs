using BuildNotifier.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildNotifier.Services.ChatSessionManagement
{
    public sealed class ChatSessionWithCancellation
    {
        public CancellationTokenSource CancellationTokenSource { get; } = new();
        public IChatSession Session { get; }

        public ChatSessionWithCancellation(IChatSession session)
        {
            Session = session;
        }

        public void CancelAndDispose()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }
    }
}

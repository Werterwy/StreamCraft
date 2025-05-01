using System.Threading.Channels;

namespace StreamCraftAPI.Queues
{
    public class VideoProcessingQueue
    {

        private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

        public async Task EnqueueAsync(Guid videoId)
        {
            await _queue.Writer.WriteAsync(videoId);
        }

        public async Task<Guid> DequeueAsync(CancellationToken cancellationToken)
        {
            var videoId = await _queue.Reader.ReadAsync(cancellationToken);
            return videoId;
        }

        /*private readonly Queue<Guid> _videoIds = new();
        private readonly object _lock = new();

        public void Enqueue(Guid videoId)
        {
            lock (_lock)
            {
                _videoIds.Enqueue(videoId);
            }
        }

        public Guid? Dequeue()
        {
            lock (_lock)
            {
                if (_videoIds.Count == 0)
                    return null;
                return _videoIds.Dequeue();
            }
        }*/


    }
}

using System.Threading.Channels;

namespace StreamCraftAPI.Queues
{
    public class VideoProcessingQueue
    {

        private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

        public async Task EnqueueAsync(Guid TaskId)
        {
            await _queue.Writer.WriteAsync(TaskId);
        }

        public async Task<Guid> DequeueAsync(CancellationToken cancellationToken)
        {
            var TaskId = await _queue.Reader.ReadAsync(cancellationToken);
            return TaskId;
        }
    }
}

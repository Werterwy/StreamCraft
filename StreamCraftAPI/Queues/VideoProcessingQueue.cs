namespace StreamCraftAPI.Queues
{
    public class VideoProcessingQueue
    {
        private readonly Queue<Guid> _videoIds = new();
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
        }
    }
}

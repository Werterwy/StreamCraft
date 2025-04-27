namespace StreamCraftAPI.Queues
{
    public class VideoProcessingQueue
    {
        private readonly Queue<int> _videoIds = new();
        private readonly object _lock = new();

        public void Enqueue(int videoId)
        {
            lock (_lock)
            {
                _videoIds.Enqueue(videoId);
            }
        }

        public int? Dequeue()
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

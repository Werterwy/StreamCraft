using StreamCraftAPI.Data.Model;

namespace StreamCraftAPI.Data.Entities
{
    public class VideoProcessingTask
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public VideoTaskType TaskType { get; set; }
        public StreamCraftAPI.Data.Model.TaskStatus Status { get; set; }
        public int Attempts { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

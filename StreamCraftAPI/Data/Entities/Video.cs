using StreamCraftAPI.Data.Model;

namespace StreamCraftAPI.Data.Entities
{
    public class Video
    {
        public Guid Id { get; set; }        
        public Guid UserId { get; set; }   
        public string FilePath { get; set; }  
        public string ThumbnailPath { get; set; } 
        public VideoStatus Status { get; set; }  
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
    }

}

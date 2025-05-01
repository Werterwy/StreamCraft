using StreamCraftAPI.Data.Model;

namespace StreamCraftAPI.Data.Entities
{
    public class Video
    {
        public Guid Id { get; set; }        
        public Guid UserId { get; set; }   
        public string FilePath { get; set; }  
        public string ThumbnailPath { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public string FilePath720 { get; set; }
        public string FilePath1080 { get; set; }
        
    }

}

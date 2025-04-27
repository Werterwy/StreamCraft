namespace StreamCraftAPI.Data.Entities
{
    public class Video
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string TempPath { get; set; }
        public DateTime UploadTime { get; set; }
        public string Status { get; set; }
    }
}

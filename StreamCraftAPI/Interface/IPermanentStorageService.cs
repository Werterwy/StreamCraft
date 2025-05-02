namespace StreamCraftAPI.Interface
{
    public interface IPermanentStorageService
    {
        Task<string> UploadVideoAsync(string localFilePath, string fileName);
        Task<string> UploadThumbnailAsync(string localFilePath, string fileName);
    }

}

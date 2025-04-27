namespace StreamCraftAPI.Data.Model
{
    public enum VideoStatus
    {
        Uploaded = 0,      // Пользователь загрузил видео
        Processing = 1,    // Видео в процессе обработки (конвертация, превью)
        Processed = 2,     // Видео обработано успешно
        Failed = 3         // Ошибка при обработке
    }

}

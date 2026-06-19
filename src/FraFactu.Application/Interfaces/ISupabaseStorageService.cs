namespace FraFactu.Application.Interfaces
{
    public interface ISupabaseStorageService
    {
        Task<string> UploadLogoAsync(Stream fileStream, string fileName, string userId);
        Task<string> UploadSystemLogoAsync(Stream fileStream, string fileName);
        string GetSystemLogoUrl();
    }
}

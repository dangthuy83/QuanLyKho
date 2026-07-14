using Microsoft.AspNetCore.Http;

namespace KhoQuanLy.Services;

public interface IPdfStorageService
{
    Task<StoredPdfFile> SavePspPdfAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public sealed class StoredPdfFile
{
    public string FileName { get; set; } = "";
    public string PhysicalPath { get; set; } = "";
    public string LogicalPath { get; set; } = "";
}

public class PdfStorageService : IPdfStorageService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public PdfStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<StoredPdfFile> SavePspPdfAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File PDF không hợp lệ.", nameof(file));

        var storageRoot = GetPspStorageRoot();
        Directory.CreateDirectory(storageRoot);

        var safeOriginalName = Path.GetFileName(file.FileName);
        var fileName = $"{Guid.NewGuid():N}_{safeOriginalName}";
        var physicalPath = Path.Combine(storageRoot, fileName);

        await using (var fs = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(fs, cancellationToken);
        }

        return new StoredPdfFile
        {
            FileName = fileName,
            PhysicalPath = physicalPath,
            LogicalPath = $"psp-pdf/{fileName}"
        };
    }

    private string GetPspStorageRoot()
    {
        var configuredPath = _configuration["Storage:PspPdfPath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath));

        // Fallback nằm ngoài wwwroot để tránh public trực tiếp file PDF.
        return Path.Combine(_environment.ContentRootPath, "App_Data", "PspPdf");
    }
}
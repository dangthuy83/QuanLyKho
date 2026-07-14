using System.Text.Json;
using System.Text.Json.Nodes;

namespace KhoQuanLy.Services;

public interface IConfigSettingsService
{
    string GetPspPdfPath();
    string GetActualPspPdfPath();
    void SavePspPdfPath(string path);
}

public class ConfigSettingsService : IConfigSettingsService
{
    private readonly IWebHostEnvironment _env;

    public ConfigSettingsService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string GetPspPdfPath()
    {
        var configPath = GetConfigPath();
        var json = JsonNode.Parse(File.ReadAllText(configPath));
        return json?["Storage"]?["PspPdfPath"]?.GetValue<string>() ?? "";
    }

    public string GetActualPspPdfPath()
    {
        var configured = GetPspPdfPath();
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured));
        return Path.Combine(_env.ContentRootPath, "App_Data", "PspPdf");
    }

    public void SavePspPdfPath(string path)
    {
        var actualPath = string.IsNullOrWhiteSpace(path)
            ? Path.Combine(_env.ContentRootPath, "App_Data", "PspPdf")
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        Directory.CreateDirectory(actualPath);

        var configPath = GetConfigPath();
        var json = JsonNode.Parse(File.ReadAllText(configPath));
        if (json?["Storage"] is JsonObject storage)
        {
            storage["PspPdfPath"] = path;
        }
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, json!.ToJsonString(options));
    }

    private string GetConfigPath()
        => Path.Combine(_env.ContentRootPath, "appsettings.json");
}

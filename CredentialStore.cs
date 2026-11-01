using System.IO;
using System.Text.Json;

namespace SPTAutoLogin;

/// <summary>
/// 凭据存储：以 JSON 明文形式保存在当前用户的 AppData 目录中。
/// </summary>
internal static class CredentialStore
{
    private sealed class CredentialData
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    private static string FilePath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SPT一键登录");
            return Path.Combine(dir, "credential.json");
        }
    }

    public static (string Username, string Password)? Load()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                return null;
            }

            CredentialData? data = JsonSerializer.Deserialize<CredentialData>(
                File.ReadAllText(path));

            if (data is null || string.IsNullOrEmpty(data.Username))
            {
                return null;
            }

            return (data.Username, data.Password);
        }
        catch
        {
            // 文件损坏等情况下视为无保存凭据
            return null;
        }
    }

    public static void Save(string username, string password)
    {
        string path = FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string json = JsonSerializer.Serialize(
            new CredentialData { Username = username, Password = password });
        File.WriteAllText(path, json);
    }

    public static void Clear()
    {
        try
        {
            string path = FilePath;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // 删除失败时忽略
        }
    }
}

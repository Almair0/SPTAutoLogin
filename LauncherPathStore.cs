using System.IO;
using System.Text.Json;

namespace SPTAutoLogin;

/// <summary>
/// 管理用户手动选择的 SPT.Launcher.exe 位置，保存在本机配置文件中。
/// </summary>
internal static class LauncherPathStore
{
    private sealed class ConfigData
    {
        public string LauncherExe { get; set; } = "";
    }

    private static string FilePath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SPT一键登录");
            return Path.Combine(dir, "config.json");
        }
    }

    /// <summary>
    /// 获取已保存的启动器路径；未保存或文件已失效时返回 null。
    /// </summary>
    public static string? GetSavedPath()
    {
        try
        {
            string file = FilePath;
            if (!File.Exists(file))
            {
                return null;
            }

            ConfigData? data = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(file));
            if (data is not null && File.Exists(data.LauncherExe))
            {
                return data.LauncherExe;
            }
        }
        catch
        {
            // 配置损坏时忽略
        }

        return null;
    }

    /// <summary>
    /// 保存用户手动选择的路径。
    /// </summary>
    public static void Save(string path)
    {
        string file = FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        string json = JsonSerializer.Serialize(new ConfigData { LauncherExe = path });
        File.WriteAllText(file, json);
    }
}

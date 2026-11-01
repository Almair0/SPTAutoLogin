using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace SPTAutoLogin;

/// <summary>
/// 启动 SPT 启动器并通过 UI Automation 自动填入账号密码、点击登录。
/// </summary>
internal static class LoginRunner
{
    private const string ProcessName = "SPT.Launcher";

    private const string UserEditKeyword = "用户名";
    private const string PassEditPrefix = "密码";
    private const string LoginButtonName = "登录并载入存档";
    private const string LoggedInTitleMarker = "档案:";
    private const string InGameButtonName = "开始游戏";

    private static readonly TimeSpan TotalTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromSeconds(60);
    private const int PollIntervalMs = 1200;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SwRestore = 9;

    public sealed record Result(bool Success, string? Error = null)
    {
        public static Result Ok() => new(true);
        public static Result Fail(string error) => new(false, error);
    }

    public static Result Run(string launcherExe, string username, string password)
    {
        string launcherDir = Path.GetDirectoryName(launcherExe)!;

        bool needStart = Process.GetProcessesByName(ProcessName).Length == 0;
        if (needStart)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = launcherExe,
                WorkingDirectory = launcherDir,
                UseShellExecute = true,
            });
        }

        Stopwatch total = Stopwatch.StartNew();

        // 等待进程
        Process? proc = null;
        while (total.Elapsed < TotalTimeout)
        {
            proc = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (proc is not null)
            {
                break;
            }

            Thread.Sleep(500);
        }

        if (proc is null)
        {
            return Result.Fail("无法启动 SPT 启动器，请检查路径后重试。");
        }

        // 等待主窗口
        while (total.Elapsed < TotalTimeout)
        {
            proc.Refresh();
            if (!proc.HasExited && proc.MainWindowHandle != IntPtr.Zero)
            {
                break;
            }

            if (proc.HasExited)
            {
                return Result.Fail("SPT 启动器启动后立即退出，请查看其日志。");
            }

            Thread.Sleep(800);
        }

        proc.Refresh();
        if (proc.MainWindowHandle == IntPtr.Zero)
        {
            return Result.Fail("等待 SPT 启动器窗口超时。");
        }

        // 轮询界面状态
        bool submitted = false;
        Stopwatch loginWatch = Stopwatch.StartNew();

        while (total.Elapsed < TotalTimeout)
        {
            proc.Refresh();
            if (proc.HasExited)
            {
                return Result.Fail("SPT 启动器已退出。");
            }

            // 启动器可能因“启动时最小化”设置而处于最小化状态，
            // 最小化时 WebView2 不渲染、UIA 树为空，需先恢复窗口
            if (proc.MainWindowHandle != IntPtr.Zero && IsIconic(proc.MainWindowHandle))
            {
                ShowWindow(proc.MainWindowHandle, SwRestore);
                Thread.Sleep(PollIntervalMs);
                proc.Refresh();
            }

            AutomationElement root;
            try
            {
                root = AutomationElement.FromHandle(proc.MainWindowHandle);
            }
            catch
            {
                Thread.Sleep(PollIntervalMs);
                continue;
            }

            AutomationElementCollection all =
                root.FindAll(TreeScope.Descendants, Condition.TrueCondition);

            // 已登录判定
            if (proc.MainWindowTitle.Contains(LoggedInTitleMarker, StringComparison.Ordinal) ||
                ButtonExists(all, InGameButtonName))
            {
                SetForegroundWindow(proc.MainWindowHandle);
                return Result.Ok();
            }

            AutomationElement? userEdit = null;
            AutomationElement? passEdit = null;
            AutomationElement? loginButton = null;

            foreach (AutomationElement element in all)
            {
                string name = element.Current.Name;
                if (element.Current.ControlType == ControlType.Edit)
                {
                    if (name.Contains(UserEditKeyword, StringComparison.Ordinal))
                    {
                        userEdit = element;
                    }
                    else if (name.StartsWith(PassEditPrefix, StringComparison.Ordinal))
                    {
                        passEdit = element;
                    }
                }
                else if (element.Current.ControlType == ControlType.Button && name == LoginButtonName)
                {
                    loginButton = element;
                }
            }

            if (!submitted && userEdit is not null && passEdit is not null && loginButton is not null)
            {
                SetValueIfDifferent(userEdit, username);
                SetValueIfDifferent(passEdit, password);
                Thread.Sleep(200);
                InvokeButton(loginButton);
                submitted = true;
                loginWatch.Restart();
            }

            if (submitted && loginWatch.Elapsed > LoginTimeout)
            {
                return Result.Fail("自动登录超时。请检查账号密码是否正确、网络或服务器状态，\n可查看启动器界面上的提示。");
            }

            Thread.Sleep(PollIntervalMs);
        }

        return Result.Fail("等待登录界面超时（可能未能连接到服务器）。\n请打开启动器确认状态后重试。");
    }

    private static bool ButtonExists(AutomationElementCollection elements, string name)
    {
        foreach (AutomationElement element in elements)
        {
            if (element.Current.ControlType == ControlType.Button && element.Current.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetValueIfDifferent(AutomationElement element, string value)
    {
        ValuePattern pattern = (ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern);
        if (pattern.Current.Value != value)
        {
            pattern.SetValue(value);
        }
    }

    private static void InvokeButton(AutomationElement element)
    {
        InvokePattern pattern = (InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern);
        pattern.Invoke();
    }
}

using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SPTAutoLogin;

public partial class MainWindow : Window
{
    private string? _launcherExe;

    private static readonly Brush RememberedBrush = MakeBrush("#FF8BD68A");
    private static readonly Brush NotRememberedBrush = MakeBrush("#FFB4B4C0");

    private static Brush MakeBrush(string hex)
    {
        return (Brush)new BrushConverter().ConvertFromString(hex)!;
    }

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PasswordBox.KeyDown += OnPasswordKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 只使用用户已保存的路径；未设置时保持空白，由用户自行点击“更改”
        _launcherExe = LauncherPathStore.GetSavedPath();
        UpdatePathDisplay();

        if (string.IsNullOrEmpty(_launcherExe))
        {
            SetStatus("请点击“更改”选择 SPT.Launcher.exe 的位置。");
        }

        var saved = CredentialStore.Load();
        if (saved is { } credential)
        {
            UsernameBox.Text = credential.Username;
            PasswordBox.Password = credential.Password;
            RememberBox.IsChecked = true;
            if (!string.IsNullOrEmpty(_launcherExe))
            {
                LoginButton.Focus();
            }
        }
        else
        {
            RememberBox.IsChecked = false;
            UsernameBox.Focus();
        }

        UpdateRememberState();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        PromptChooseLauncher();
    }

    /// <summary>
    /// 弹出文件选择窗口，让用户选择 SPT.Launcher.exe。
    /// </summary>
    private void PromptChooseLauncher()
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Title = "请选择 SPT.Launcher.exe 的位置",
            Filter = "SPT 启动器 (SPT.Launcher.exe)|SPT.Launcher.exe|程序文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        };

        if (dialog.ShowDialog(this) == true)
        {
            _launcherExe = dialog.FileName;
            LauncherPathStore.Save(_launcherExe);
            UpdatePathDisplay();
            SetStatus("");
        }
    }

    private void UpdatePathDisplay()
    {
        LauncherPathBox.Text = _launcherExe ?? "";
        LauncherPathBox.ToolTip = _launcherExe;
    }

    private void OnRememberChanged(object sender, RoutedEventArgs e)
    {
        UpdateRememberState();
    }

    private void UpdateRememberState()
    {
        bool remember = RememberBox.IsChecked == true;

        if (remember)
        {
            RememberStateText.Text = "已记住账号密码";
            RememberStateText.Foreground = RememberedBrush;
        }
        else
        {
            RememberStateText.Text = "未记住账号密码";
            RememberStateText.Foreground = NotRememberedBrush;
        }
    }

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnLoginClick(sender, e);
        }
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_launcherExe) || !File.Exists(_launcherExe))
        {
            SetStatus("请先点击“更改”选择 SPT.Launcher.exe 的位置。");
            return;
        }

        string username = UsernameBox.Text.Trim();
        string password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username))
        {
            SetStatus("请输入官网用户名。");
            UsernameBox.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            SetStatus("请输入密码。");
            PasswordBox.Focus();
            return;
        }

        string launcherExe = _launcherExe;
        SetBusy(true);
        SetStatus("正在启动并自动登录…");

        LoginRunner.Result result =
            await RunOnStaThread(() => LoginRunner.Run(launcherExe, username, password));

        if (result.Success)
        {
            if (RememberBox.IsChecked == true)
            {
                CredentialStore.Save(username, password);
            }
            else
            {
                CredentialStore.Clear();
            }

            Application.Current.Shutdown();
            return;
        }

        SetStatus(result.Error ?? "登录失败。");
        SetBusy(false);
    }

    /// <summary>
    /// UI Automation 需要在 STA 线程上运行，将自动化逻辑放到独立的 STA 线程执行。
    /// </summary>
    private static Task<LoginRunner.Result> RunOnStaThread(Func<LoginRunner.Result> action)
    {
        TaskCompletionSource<LoginRunner.Result> tcs = new();

        Thread thread = new(() =>
        {
            try
            {
                tcs.SetResult(action());
            }
            catch (Exception ex)
            {
                tcs.SetResult(LoginRunner.Result.Fail($"发生未预期的错误：\n{ex.Message}"));
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        return tcs.Task;
    }

    private void SetBusy(bool busy)
    {
        LoginButton.IsEnabled = !busy;
        UsernameBox.IsEnabled = !busy;
        PasswordBox.IsEnabled = !busy;
        RememberBox.IsEnabled = !busy;
        BrowseButton.IsEnabled = !busy;
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
    }
}

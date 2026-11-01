# SPT 一键登录

一个 Windows 桌面小工具，用来给 SPT 启动器（`SPT.Launcher.exe`）自动填写账号密码并点击登录，省掉每次手动输入的步骤。

## 功能

- 记住启动器路径，下次打开自动带出
- 记住账号密码，启动时自动填充
- 一键完成整套流程：启动启动器 → 等待窗口就绪 → 填入账号密码 → 点击「登录并进入存档」→ 检测是否登录成功
- 登录成功后自动关闭本工具
- 单实例运行，重复打开不会启动第二个进程
- 深色界面，固定窗口尺寸

## 运行环境

- Windows 10 / 11（x64）
- [.NET 10 桌面运行时](https://dotnet.microsoft.com/download/dotnet/10.0)（Windows Desktop Runtime 10.0）
- 已安装 SPT，且能找到 `SPT.Launcher.exe`

## 使用

1. 打开程序，点「更改」选择 `SPT.Launcher.exe` 的位置
2. 输入用户名和密码
3. 需要下次自动填充，就勾选「记住账号密码（保存在本机）」
4. 点「一键登录」，或在密码框里按回车

程序会在后台启动启动器，等主窗口出现后通过 UI Automation 填入账号密码并点击登录按钮；检测到窗口标题包含「档案:」或出现「开始游戏」按钮，即判定登录成功，随后自动退出。

## 配置文件

两个文件都在 `%APPDATA%\SPT一键登录\` 下：

| 文件 | 内容 |
| --- | --- |
| `config.json` | 启动器路径 |
| `credential.json` | 账号与密码 |

## 从源码构建

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)：

```bat
git clone https://github.com/Almair0/SPTAutoLogin.git
cd SPTAutoLogin
dotnet build -c Release
```

发布单文件版本：

```bat
dotnet publish -c Release -r win-x64 --self-contained false
```

## 已知限制

- 依赖启动器界面的控件名称（`用户名`、`密码`、`登录并进入存档`），启动器改版后可能需要同步调整 `LoginRunner.cs` 里的常量
- 需要启动器窗口能被 UI Automation 访问；窗口处于最小化状态时会自动还原
- 启动器从启动到窗口就绪最多等待 120 秒，提交登录后最多等待 60 秒
- 仅支持 Windows

## 安全提示

勾选「记住账号密码」后，密码会以**明文 JSON** 保存在 `%APPDATA%\SPT一键登录\credential.json`，任何能读取你用户目录的程序都能看到它。共用电脑上建议不要勾选；已经保存过的，取消勾选后再登录一次即可清除。

如果需要更安全的存储方式，可以把 `CredentialStore` 改成先用 Windows DPAPI（`ProtectedData.Protect`）加密再落盘。

## 免责声明

本项目只对启动器界面做自动化输入，不修改游戏文件或账号数据，与 SPT 官方无关。使用前请自行确认符合你所使用服务的使用条款。

## 许可证

[MIT](LICENSE)

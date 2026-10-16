# 开发者构建说明

普通用户直接下载 [Releases 中的 EXE](https://github.com/PolarisFTL/ipa-clipboard/releases/latest)，不需要构建环境。

## Windows 本地构建

开发环境：Windows x64、.NET 10 SDK。首次恢复需要访问 NuGet 公共源；程序运行阶段不需要联网。发布前关闭正在运行的音标助手，避免 EXE 文件占用。

在仓库根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Publish.ps1
```

脚本优先使用 `build\dotnet\dotnet.exe`（若开发者自行准备），否则使用 PATH 中的 `dotnet`。输出：`release\IPA音标助手.exe`。

等价命令：

```powershell
dotnet publish src/IPAClipboard.csproj -c Release -r win-x64 --self-contained true -o release -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:DebugType=none
```

项目采用 `net10.0-windows`、WPF、`WinExe`、win-x64，自包含单文件、内嵌原生库，关闭裁剪和调试符号。XAML、音标JSON和图标都嵌入EXE；无第三方UI框架或运行时网络调用。

## 运行检查

在已登录的 Windows 桌面会话执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Run-Tests.ps1
```

脚本直接启动发布后的EXE，设置无效DOTNET_ROOT并从PATH移除SDK。检查剪贴板、分类搜索、收藏/历史、配置往返、缺字、置顶/迷你、原生标题栏命中和布局渲染。测试会使用系统剪贴板；测试配置位于独立目录，不修改正常使用配置。退出后由父进程再次核对剪贴板。

`--self-test <输出目录>` 是可选开发诊断参数，普通双击不会启用。GitHub的无交互构建环境只负责构建与产物检查，不声称完成真实桌面测试。

## GitHub Actions

[build.yml](.github/workflows/build.yml) 在 Actions 页面手动触发后使用 Windows runner 构建，并运行发布EXE自测。检查成功后生成EXE、便携ZIP、SHA256校验文件，再发布GitHub Release。已存在的同版本Release会保留。也可在工作流页面下载 `IPA-Clipboard-win-x64` artifact。

首次发布会导入已经审核的Git bundle源码，后续运行直接使用main源码。工作流只使用当前仓库的临时GITHUB_TOKEN，不需要额外保存个人访问令牌。

## 工程结构

- `src/MainWindow.xaml*`：主窗口、分类、搜索和交互。
- `src/Styles.xaml`：WPF界面样式。
- `src/Models.cs`：条目、偏好和视图模型。
- `src/Services.cs`：目录、原生Unicode剪贴板、配置、收藏和历史。
- `src/App.xaml.cs`：单实例与唤醒。
- `src/Data/catalog.json`：UTF-8结构化音标数据。
- `src/SelfTests.cs`、`tests/Run-Tests.ps1`：发布EXE自测。
- `assets/`：嵌入程序图标。
- `docs/`：中文教程、例图和测试范围。

数据JSON已纳入源码，普通构建不需要Python。可选的 `build/create_catalog.py` 用于重新生成目录；编辑数据后需重建EXE。数据归属见 [SOURCES.md](SOURCES.md)。

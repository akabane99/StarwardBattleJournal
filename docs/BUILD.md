# 从源码构建

构建脚本以自身位置定位文件，可以在任意工作目录调用。输出保存在被 Git 忽略的 `artifacts/`，中间文件保存在 `.build/`。

## Windows

需要 Windows、PowerShell 和 .NET Framework 4.x 编译器。应用使用 Windows Forms 和系统程序集，无需下载 NuGet 包。

```powershell
pwsh -File ./desktop/build.ps1
pwsh -File ./tests/run-desktop.ps1
```

成品为 `artifacts/windows/starward-battle-journal/`。完整打包该文件夹；角色头像和官网助手是必要资源。构建脚本只复制静态资源，不读取运行目录的战绩或浏览器数据。

## Android

需要 Windows、PowerShell、JDK 17 或更新版本、Android SDK Platform 35 和 SDK Build Tools。构建采用 `aapt2`、`javac`、D8、`zipalign` 与 `apksigner`，不依赖 Gradle。

设置 `JAVA_HOME` 和 `ANDROID_SDK_ROOT` 后执行：

```powershell
pwsh -File ./android/build.ps1
```

默认输出 `artifacts/android/starward-battle-journal-unsigned.apk`，需签名后才能安装。若工具不在常规 SDK 目录，可使用 `-JdkPath`、`-BuildToolsPath` 和 `-PlatformPath` 显式指定。

发布签名示例（文件保存在仓库外，示例路径需替换）：

```powershell
pwsh -File ./android/build.ps1 `
  -Keystore 'E:/private/starward/battle.jks' `
  -KeyAlias 'battle' `
  -StorePasswordFile 'E:/private/starward/store-password.txt'
```

密钥密码不同于仓库密码时，再指定 `-KeyPasswordFile`。脚本不将密码写入命令参数或构建日志。正式版升级必须继续使用原有签名密钥；不要把私钥和密码上传仓库。

## JavaScript 数据处理检查

需要 Node.js 18 或更新版本，不需要第三方 npm 依赖：

```powershell
node ./tests/core-smoke.cjs
```

测试仅使用仓库内的合成演示数据，不调用官网、不需要登录。原开发目录中依赖真实战绩的测试、临时升级脚本及私有样本未加入仓库。

## 自动构建

`.github/workflows/windows.yml` 在推送或 Pull Request 时执行合成数据测试并生成 Windows 构建产物，可在 GitHub Actions 的对应运行中下载。工作流不自动上传 Release，也不访问任何个人账号。

# 发布到 GitHub

此文件夹是战绩分析软件的干净源码项目，包含 Windows 和 Android 源码、素材、官网辅助脚本、构建文档与合成测试；不包含按键管理器或每周天梯分析项目。

## 创建仓库并推送

在 GitHub 创建空仓库，建议名称 `starward-battle-journal`。在此目录运行（将最后两条的仓库地址换成自己的）：

```powershell
git add .
git commit -m "Initial Starward Battle Journal source"
git remote add origin https://github.com/YOUR-ACCOUNT/starward-battle-journal.git
git push -u origin main
```

整理后的目录已初始化 Git 的 `main` 分支。若使用源码 ZIP 解压版本，应先执行 `git init -b main`。提交所用的姓名和邮箱由你本机 Git 配置决定，本项目未替你设置。

也可以通过 GitHub 上传文件页面上传源码。务必一并上传 `.gitignore`、`.gitattributes` 和 `.github/`。

## 安装包放入 Releases

仓库保留源码，不提交 EXE、APK、SDK 或浏览器资料。创建 Release 后，上传以下现有安装包：

- Windows：`星之翼战绩统计-v1.13.zip`
- Android：`星之翼战绩分析-安卓-v1.13.apk`

这两个成品文件保存在整理目录的同级 `outputs` 目录。Android 与 PC 的后续版本号可以独立递增。源码构建方法见 [BUILD.md](BUILD.md)。

## 已排除的内容

原有战绩 JSON、浏览器 Cookie 与登录资料、账号网页下载记录、排行榜缓存、界面偏好、签名密钥与密码、临时脚本、测试日志、绝对个人路径、工具下载和旧安装包均未复制到发布目录。原开发目录和你的本机数据保持原样。

`.gitignore` 也屏蔽常见运行数据和签名文件。导入真实战绩请使用程序运行目录；不要将原始战绩放入源码仓库。

## 许可

当前没有擅自添加 MIT、GPL 等代码许可证。请按发布意图自行选择代码许可证，并保留 [NOTICE.md](../NOTICE.md) 中对第三方游戏素材的说明。

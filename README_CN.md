中文 | [EN](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/blob/main/README.md)

# HappyGenyuanImsactUpdate
A hdiff-using update program of a certain anime game.   

## 公告
### 许可证更改通知
自 2023 年 8 月 30 日 起，本项目已经更改为 MIT 许可证。所有以前和未来的贡献都受此新许可证约束。

### [请勿使用该程序来更新至3.6版本](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues/15)

从3.6开始，miHoYo 将 `StreamingAssets/Audio/GeneratedSoundBanks/Windows` 更改为 `StreamingAssets/AudioAssets`，但由启动器负责修改，不包含在更新包中。

这不会被修复，因为代码可能面临被污染的风险。

这很可能是一个临时的特例，**此更新程序在 3.7 及以后的版本中仍可用**。有关详细信息，请转至 [该 issue](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues/15)。

### 关于有限支持的说明

自 4.6 版本起，HoYoPlay 逐渐将 `sophon chunk` 作为其主要的更新分发与安装模式。

本程序仅对 hdiff 更新包适配，故不会添加对相应功能的支持；但本软件仍然会进行基本 Bug 修复与优化的更新（如果必要）。

## 新版本特性
### v3.2.4
- 在使用 `Updater` 时，目录下如果不存在（可被程序识别的）游戏可执行文件，允许用户手动二次确认继续。

### v3.2.3
- 由于提供的 `7z.exe`、`hpatchz.exe` 与 `hdiffz.exe` 均为 64 位程序，取消了对于 32 位 Windows 的支持计划。

#### Patch Creator
- 更新了 `--only-include-pkg-defined-files`、`--include-audios` 选项。有关详细信息，请参阅 [如何使用 - 创建更新包 / Patch Creater](#创建更新包--patch-creater).

### v3.2.2
#### Updater
- 修复了 Updater 无法正确支持 崩坏：整活铁道 的问题。
- 软件现在打包 .NET 6.0 运行时发布。
- 在发行版中将会支持 32 位 Windows。
- 在 Windows 7 上添加了右下角气泡提示。Windows 10 上此功能仍然表现为“通知”。

### v3.2.1
#### Updater
- 修复了 Updater 在 <=1.5 版本下异常提示无法检查音频包完整性的问题。
- 修复了 Updater 在 >=3.6 版本下无法检测到已安装语音包的问题。
- 添加了对 崩坏：整活铁道 更新包的支持。

#### Patch Creator
现在，当程序检测到提供的两个版本文件夹包含的文件名称相同时，会请求用户检查提供的参数是否正确。

### v3.2.0
- 将整个项目迁移到了 `EggEgg.CSharp-Logger v3.0.0`。
  现在无论是更新还是创建更新包都可以有日志记录保留在程序目录下，但调试日志不会显示在控制台而会输出在 `latest.debug.log` 中。
- 在更新时，hdiff patch 失败支持进行自动重试（3 次）。
- 如果不解压程序直接运行现在会触发警告。

### v3.0.0
现在您可以像官方一样自己创建更新包了！    
使用命令行调用 `Patch Creater\HDiffPatchCreator.exe` 即可。

注意：强烈建议**仅使用原本的官方包为源文件**创建更新包。

供您自己电脑上使用的文件可能会包含小更新和缓存内容，使用包的人可能并不具备这些文件。**将缓存放入包内甚至可能导致您的个人信息泄露。**

您可以前往这里下载来自官方的文件：

- [Anime Game Downloads Archive](https://git.xeondev.com/YYHEggEgg/GI-Download-Library)
- [Honkai: March 7th Downloads Archive](https://github.com/keitarogg/HSR-Download-Library)

## 如何找到游戏目录文件夹    
1. 打开启动器   
2. 点击“启动”旁的菜单    
![Launcher UI](https://raw.githubusercontent.com/YYHEggEgg/HappyGenyuanImsactUpdate/main/Tutorial%20Images/rel_v2.1.2%2B/img01.jpg)    

3. 点击“安装位置”选项
4. 找到启动器显示的文件夹 (**注意：图片仅供参考，目录在您自己的电脑上与图片中不同！**)
![Installation Location](https://raw.githubusercontent.com/YYHEggEgg/HappyGenyuanImsactUpdate/main/Tutorial%20Images/rel_v2.1.2%2B/img02.jpg)  

如果您已从官方启动器预下载了更新文件，也可以在内看到两个更新包（通常为 `.zip` 文件）。

## 如何使用
### 补丁工具使用 / Updater
你需要以下文件:

- 游戏文件
- 一个或多个zip更新包
- 在 [release](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/releases) 下载的本程序最新发行版

您可以参考这里的指示使用程序。     
首先，您需要将游戏目录（目录下有 Yuansact.exe 或 GenyuanImsact.exe）输入程序，         
其次，程序会询问您是否要在更新后检查文件正确性。
- 输入0 - 请勿进行任何检查
- 输入1 - _（推荐使用）_ 仅检查文件大小是否符合预期（过程一般在 10s 以内，大多数情况下足够）
- 输入2 - 获取 MD5 进行完全检查（速度取决于硬盘性能，如果游戏数据没有存放在 SSD 之类的高速驱动器上将会需要很长时间）

然后输入您的更新包（通常是 zip 文件）**数量**。     
在这之后，您只需要依次拖入所有更新包（每拖入一个包需要回车确认）即可开始更新。

通常，在更新完成后，如果您使用从官方启动器下载的游戏版本，程序将会指导您使官方启动器显示正确的游戏版本。     
程序会告知您检测到的版本更新状态，如果版本正确只需按 `y` 确认即可，不想进行更改也可按 `n` 取消。       
如果程序显示的版本不正确，您也可以输入正确的版本并继续。

Enjoy it!

### 创建更新包 / Patch Creater
你需要以下文件：

- **从官方下载的**两个版本的**已解压游戏文件**
- 在 [release](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/releases) 下载的本程序最新发行版

您可以参考这里的命令行使用指南。     
```
Usage: hdiffpatchcreator
  -from <更新前版本> <所在文件夹>
  -to <更新后版本> <所在文件夹>
  -output_to <输出文件夹，存放更新包和临时文件>
  [-p <前缀>] [-reverse] [--skip-check]
  [--only-include-pkg-defined-files [--include-audios]]
```

使用程序可以得到这样一个更新包：
```
[前缀]_<更新前版本>_<更新后版本>_hdiff_<16位随机字符串>.zip
```
如 `game_3.4_8.0_hdiff_nj89iGjh4d.zip`
前缀默认为 `game`.

`-reverse` 选项在创建更新包后，更换“更新前版本”与“更新后版本”并再创建一个包。

`--skip-check` 选项跳过基于文件大小比较的基础检查。但注意：对于 Patch Creator，由于需要进行严格的文件比较，程序一定会对文件进行 MD5 计算。建议您在读写速度较快的存储设备上（如 SSD）进行创建更新包的操作。

`--only-include-pkg-defined-files` 可以在创建更新包时忽略 `pkg_version` 定义以外的所有文件，以避免本地的热更新、缓存、错误日志等无关内容被包括在更新包中。  
`--only-include-pkg-defined-files` 并不包括 `Audio_*_pkg_version` 定义的特定语言配音音频包文件。如果需要包含它们，请指定 `--include-audios` 选项。

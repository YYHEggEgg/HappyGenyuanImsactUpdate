中文 | [EN](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/blob/main/README.md)

# HappyGenyuanImsactUpdate
A hdiff-using update program of a certain anime game.

## 公告

### [请勿使用该程序来更新至3.6版本](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues/15)

从3.6开始，miHoYo 将 `StreamingAssets/Audio/GeneratedSoundBanks/Windows` 更改为 `StreamingAssets/AudioAssets`，但由启动器负责修改，不包含在更新包中。

这不会被修复，因为代码可能面临被污染的风险。

这很可能是一个临时的特例，**此更新程序在 3.7 及以后的版本中仍可用**。有关详细信息，请转至 [该 issue](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues/15)。 

## 新版本特性
### v3.2.0
- 将整个项目迁移到了 `EggEgg.CSharp-Logger v3.0.0`。
  现在无论是更新还是创建更新包都可以有日志记录保留在程序目录下，但调试日志不会显示在控制台而会输出在 `latest.debug.log` 中。
- 在更新时，hdiff patch 失败支持进行自动重试（3 次）。
- 如果不解压程序直接运行现在会触发警告。

### v3.1.0
修复了一些 bug，现在程序不会删除并没有更新的 *_pkg_version了。

### v3.0.0
现在您可以像官方一样自己创建更新包了！    
使用命令行调用 `Patch Creater\HDiffPatchCreator.exe` 即可。

注意：强烈建议**仅使用原本的官方包为源文件**创建更新包。

供您自己电脑上使用的文件可能会包含小更新和缓存内容，使用包的人可能并不具备这些文件。**将缓存放入包内甚至可能导致您的个人信息泄露。**

您可以前往这里下载来自官方的文件： [Downloads Archive](https://github.com/Angoks/GI-Download-Library)

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
```

使用程序可以得到这样一个更新包：
```
[前缀]_<更新前版本>_<更新后版本>_hdiff_<16位随机字符串>.zip
```
如 `game_3.4_8.0_hdiff_nj89iGjh4d.zip`
前缀默认为 `game`.
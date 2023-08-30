[中文](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/blob/main/README_CN.md) | EN

# HappyGenyuanImsactUpdate
A hdiff-using update program of a certain anime game.

## Annoucements
### License Change Notice
As of Aug 30, 2023, this project has been re-licensed under the MIT License. All previous and future contributions are subject to this new license.

### [Don't use this for ->3.6 Update](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues/15)
From 3.6, miHoYo changed `StreamingAssets/Audio/GeneratedSoundBanks/Windows` to `StreamingAssets/AudioAssets`, but the launcher is responsible for the modification, not the update package.  

This won't be fixed as I don't want to pollute the code any more. 

This is most probably a temporaily a corner case and **this Updater program is still avaliable in >=3.7 versions**. For more information, go to [this issue](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues/15).

## New feature
### v3.2.2
#### Updater
- Fixed the issue where the Updater won't work with `Honkai: March 7th`.
- The software is now packed with dotnet runtime (6.0), allowing user not to install runtime.
- Release version supported Windows x86.
- Supported balloon tip notice on Windows 7 and newer (notifications instead on >= Windows 10).

### v3.2.1
#### Updater
- Fixed the issue where the Updater won't work with anime game version <= `1.5`.
- Fixed an issue with Audio packages where the Updater won't detect them in anime game version >= `3.6`.
- Supported packages from `Honkai: March 7th`.

#### Patch Creator
Now, when the Patch Creator detected that the given two directories have files with the same names, it will request the user for review and confirmation.

### v3.2.0
- Migrated the entire project to `EggEgg.CSharp-Logger v3.0.0`.
  Now, whether updating or creating update packages, the logs will be kept in the program directory, but the debug logs will not be displayed on the console and will be output to `latest.debug.log`.
- During updates, hdiff patch failure is supported for automatic retries (3 times).
- A warning will be triggered if the program is run without extracting it.

### v3.0.0
Now you can create hdiff patch packages on your own, like `the anime game company`!   
Just invoke `Patch Creater\HDiffPatchCreator.exe` in command line.

Notice: It's highly recommended to **use original packages from the anime game company only** to create patches.

Files from your own computer will probably contains live updates and caches, which some users don't have. **Putting caches into the package will be likely to make your personal information got leaked.**

You can turn to this repository to download files from `the anime game company`: 

- [Anime Game Downloads Archive](https://github.com/Angoks/GI-Download-Library)
- [Honkai: March 7th Downloads Archive](https://github.com/keitarogg/HSR-Download-Library)

## Usage
### How to use the patcher / Updater
You should have the following things:

- A game (for sure)
- One or more upgrade packages (zip file)
- A release of this program

You can use it by the instruction here or in the program.     
First of all, it will ask for the full path of game directory.      
Next, it will ask you to choose how to check the files after update:   
- 0 - Don't have any check
- 1 - _(Recommended)_ Only check file size (usually < 10s, very fast, in most cases enough)
- 2 - Full check on MD5 (the speed depends on your disk, it will take a long time if the data isn't on a fast-speed drive like SSD)

Then, you need to type how many zip files you have.     
After that, you just need to drag zip files one by one (press enter after dragging in), then the update program will finish the update process automatically.

Enjoy it!

### How to create a patch / Patch Creater
You can refer to the following command line usage.
```
Usage: hdiffpatchcreator
  -from <versionFrom> <source_directory>
  -to <versionTo> <target_directory>
  -output_to <output_zip_directory>
  [-p <prefix>] [-reverse] [--skip-check]
```
  
By using this program, you can get a package named: 
```
[prefix]_<versionFrom>_<versionTo>_hdiff_<randomstr>.zip
```
e.g. `game_3.4_8.0_hdiff_nj89iGjh4d.zip`
If not given, prefix will be `game`.
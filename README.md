[中文](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/blob/main/README_CN.md) | EN

# HappyGenyuanImsactUpdate
A hdiff-using update program of a certain anime game.

## New feature
### v3.1.0
#### Updater
Have some bugfix about pkg_version reserve and verify. 

### v3.0.0
Now you can create hdiff patch packages on your own, like `the anime game company`!   
Just invoke `Patch Creater\HDiffPatchCreator.exe` in command line.

Notice: It's highly recommended to **use original packages from the anime game company only** to create patches.

Files from your own computer will probably contains live updates and caches, which some users don't have. **Putting caches into the package will be likely to make your personal information got leaked.**

You can turn to this repository to download files from `the anime game company`: [Downloads Archive](https://github.com/Angoks/GI-Download-Library)

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
After that, you just need to paste all paths of zip files at a time, then the update program will finish the update process automatically.

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
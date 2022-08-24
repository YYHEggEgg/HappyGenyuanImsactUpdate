[中文](https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/blob/main/README_CN.md) | EN

# HappyGenyuanImsactUpdate
A hdiff-using update program of a certain anime game.

## New feature
### v2.1.1   
- If you're using an official version, the update package are usually located in the game folder.       
  We have added support in this case. Now, when you're pasting the name of `zip` file, we'll think of it as a **relative path**, which is located in the game data folder.
- Now, the program will ask you whether to delete the update package. They aren't needed after update.

### v2.0
Now, if you are using an official version (downloaded by launcher), the program will help you change `config.ini` to make launcher display the correct version.    
In most cases, the program can automatically judge the version you're updating to, but it needs you to confirm.    
If it happens, the program will send a message (on Windows 10), so you can still minimum the console, do other things, and you'll receive message if needed.    

In v2.0+, You'll also receive message if update process has been finished, so you can know it at once.

## Usage
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

mkdir .\Release-windows10-x64
mkdir .\Release-windows10-x64\Updater
xcopy .\HappyGenyuanImsactUpdate\bin\Release\net6.0-windows10.0.17763.0 .\Release-windows10-x64\Updater\ /s /e /y
mkdir ".\Release-windows10-x64\Patch Creator"
xcopy .\HDiffPatchCreator\bin\Release\net6.0-windows10.0.17763.0 ".\Release-windows10-x64\Patch Creator\" /s /e /y

pause
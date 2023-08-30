mkdir .\Release-windows7-x86
dotnet publish --configuration Release .\HappyGenyuanImsactUpdate --arch x86 --output .\Release-windows7-x86\Updater
copy ".\HappyGenyuanImsactUpdate\bin\Release\net6.0-windows7.0\7z.exe" ".\Release-windows7-x86\Updater\7z.exe" /y
copy ".\HappyGenyuanImsactUpdate\bin\Release\net6.0-windows7.0\hpatchz.exe" ".\Release-windows7-x86\Updater\hpatchz.exe" /y

dotnet publish --configuration Release .\HDiffPatchCreator --arch x86 --output ".\Release-windows7-x86\Patch Creator"
copy ".\HDiffPatchCreator\bin\Release\net6.0-windows7.0\7z.exe" ".\Release-windows7-x86\Patch Creator\7z.exe" /y
copy ".\HDiffPatchCreator\bin\Release\net6.0-windows7.0\hdiffz.exe" ".\Release-windows7-x86\Patch Creator\hdiffz.exe" /y
copy ".\HDiffPatchCreator\bin\Release\net6.0-windows7.0\hpatchz.exe" ".\Release-windows7-x86\Patch Creator\hpatchz.exe" /y

mkdir .\Updater-windows7-x86-packed-runtime
dotnet publish --configuration Release --runtime win7-x86 .\HappyGenyuanImsactUpdate --output .\Updater-windows7-x86-packed-runtime\bin
copy ".\HappyGenyuanImsactUpdate\bin\Release\net6.0-windows7.0\7z.exe" ".\Updater-windows7-x86-packed-runtime\bin\7z.exe" /y
copy ".\HappyGenyuanImsactUpdate\bin\Release\net6.0-windows7.0\hpatchz.exe" ".\Updater-windows7-x86-packed-runtime\bin\hpatchz.exe" /y

pause
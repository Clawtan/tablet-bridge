@echo off
echo Compiling TabletBridge...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:TabletBridge.exe TabletBridge.cs
echo Build Complete!
pause

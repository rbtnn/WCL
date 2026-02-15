@echo on
set SRC=CommandItem.cs LauncherForm.cs LauncherItemRow.cs LauncherModel.cs
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /win32icon:app.ico /target:winexe /out:WCL.exe %SRC%
pause

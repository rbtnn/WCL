@echo on
set PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319
csc /target:winexe /nologo /out:TypedPathsSync.exe Prog.cs
pause

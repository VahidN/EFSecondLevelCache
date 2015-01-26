"%~dp0NuGet.exe" pack "..\EFSecondLevelCache\EFSecondLevelCache.csproj" -Prop Configuration=Release
copy "%~dp0*.nupkg" "%localappdata%\NuGet\Cache"
pause
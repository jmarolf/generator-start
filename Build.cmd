@echo off
dotnet tool restore
dotnet msbuild -noLogo -v:m -m -r -bl:artifacts/log/build.binlog 
dotnet pack --no-build --no-restore --nologo --output artifacts/packages
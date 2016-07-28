"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe" ..\BuildAzure.IoT.Adafruit.BME280.csproj /target:Clean;Build /p:Configuration=Release /p:Platform=AnyCPU

if exist lib (rmdir contents\lib /s /q)
mkdir contents\lib\uap10.0

copy ..\bin\Release\*.pri contents\]lib\uap10.0
copy ..\bin\Release\*.dll contents\lib\uap10.0

nuget pack contents\BuildAzure.IoT.Adafruit.BME280.nuspec

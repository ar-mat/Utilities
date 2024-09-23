# Run in release mode
param($Configuration = "Release")

# Get the original directoty, it will get back there after it's done
$OriginalDir=Get-Location | select -ExpandProperty Path

# Go to the project directory
cd ../Projects/Utils

# Tagret path of published artifacts
$BuildPath = "../../bin/$Configuration"
$TargetPath = "$BuildPath/publish/Utils"

#Build the project
dotnet build Utils.csproj -c $Configuration -o $BuildPath

# Publish artifacts
dotnet publish Utils.csproj -c $Configuration --no-build -o $TargetPath /p:OutputPath=$BuildPath

# Zip the contents
Compress-Archive -Path $TargetPath -DestinationPath $TargetPath\Armat.Utils-1.0.1.zip -Force

# Go back to the original directory
cd $OriginalDir
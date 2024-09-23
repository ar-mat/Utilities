# Run in release mode
param($Configuration = "Release")

# Get the original directoty, it will get back there after it's done
$OriginalDir=Get-Location | select -ExpandProperty Path

# Go to the project directory
cd ../Projects/Utils

# Tagret path of published artifacts
$TargetPath = "../../bin/$Configuration/publish/Utils"

# Publish artifacts
dotnet publish -c $Configuration -o $TargetPath

# Zip the contents
Compress-Archive -Path $TargetPath -DestinationPath $TargetPath\Armat.Utils-1.0.1.zip -Force

# Go back to the original directory
cd $OriginalDir
# Run in release mode
param($Configuration = "Release")

# Get the original directoty, it will get back there after it's done
$OriginalDir=Get-Location | select -ExpandProperty Path

# Go to the project directory
cd ../Projects/Utils

#Build the project
dotnet build Utils.csproj -c $Configuration -o "$OriginalDir/../bin/$Configuration"

# Pack nuget artifacts
dotnet pack Utils.csproj -c $Configuration --no-build -o "$OriginalDir/../bin/$Configuration" /p:OutputPath="$OriginalDir/../bin/$Configuration"

# Go back to the original directory
cd $OriginalDir
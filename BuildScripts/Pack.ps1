# Run in release mode
param($Configuration = "Release")

# Get the original directoty, it will get back there after it's done
$OriginalDir=Get-Location | select -ExpandProperty Path

# Go to the project directory
cd ../Projects/Utils

# Tagret path of published artifacts
$BuildPath = "../../bin/$Configuration"

#Build the project
dotnet build Utils.csproj -c $Configuration -o $BuildPath

# Pack nuget artifacts
dotnet pack Utils.csproj -c $Configuration --no-build -o $BuildPath /p:OutputPath=$BuildPath

# Go back to the original directory
cd $OriginalDir
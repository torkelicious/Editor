#!/bin/bash

# Check if dotnet is installed
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Error: dotnet CLI is not installed. Please install the .NET SDK first."
  exit 1
fi

# Check if pwsh is installed
if command -v pwsh >/dev/null 2>&1; then
  echo "pwsh found"
else
  echo "pwsh not found, installing now..."
  dotnet tool install --global PowerShell
fi

# Try run pwsh from PATH otherwise run from dotnet tools folder
if command -v pwsh >/dev/null 2>&1; then
  pwsh ./build.ps1 "$@"
else
  echo "pwsh not in PATH, running from dotnet tools directory"
  "$HOME/.dotnet/tools/pwsh" ./build.ps1 "$@"
fi

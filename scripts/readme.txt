These scripts build and publish/build the project from source.

---

build.ps1
    pwsh script for building/publishing.
    Arguments:
      -LinuxNative   Linux Native AOT (self-contained, trimmed, single file)
      -Portable      Framework-dependent (cross-platform, no runtime)
      -Win           Windows self-contained (trimmed, single file, compressed)
      -Linux         Linux self-contained (trimmed, single file, compressed)
      -ProjectPath   Optional path to .csproj file

---

build.sh
    Bash wrapper for build.ps1.
    - Verifies .NET (dotnet CLI) & PowerShell Core (pwsh) is installed
    - installs both dotnet if missing
    - Passes all CLI args to build.ps1

---

Usage:

    build.ps1:
    pwsh build.ps1 [targets] [-ProjectPath path/to/project.csproj]


    build.sh:
    chmod +x ./scripts/build.sh
    ./build.sh [targets] [-ProjectPath path/to/project.csproj]

---

Targets:
    -LinuxNative   Linux Native AOT
    -Portable      Framework-dependent
    -Win           Windows self-contained
    -Linux         Linux self-contained
    (If no targets are given, all are built)

---

Output:
    ~/Publish/tgent/
    (Each target is in its own cleaned subfolder)

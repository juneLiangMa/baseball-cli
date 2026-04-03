# Building Baseball CLI

This document explains how to build and distribute baseball-cli across different platforms.

## Quick Start

### Prerequisites
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) or later
- Git

### Build Locally

```bash
cd baseball-cli

# Restore dependencies
dotnet restore

# Build and publish for your current platform
dotnet publish -c Release --self-contained
```

The executable will be in `bin/Release/net6.0/*/publish/`

## Cross-Platform Builds

To build for multiple platforms, use the Runtime Identifier (RID):

### Windows (64-bit)
```bash
dotnet publish -c Release -r win-x64 --self-contained
# Output: bin/Release/net6.0/win-x64/publish/baseball-cli.exe
```

### Linux (64-bit)
```bash
dotnet publish -c Release -r linux-x64 --self-contained
# Output: bin/Release/net6.0/linux-x64/publish/baseball-cli
chmod +x bin/Release/net6.0/linux-x64/publish/baseball-cli
```

### macOS (Intel)
```bash
dotnet publish -c Release -r osx-x64 --self-contained
# Output: bin/Release/net6.0/osx-x64/publish/baseball-cli
chmod +x bin/Release/net6.0/osx-x64/publish/baseball-cli
```

### macOS (Apple Silicon)
```bash
dotnet publish -c Release -r osx-arm64 --self-contained
# Output: bin/Release/net6.0/osx-arm64/publish/baseball-cli
chmod +x bin/Release/net6.0/osx-arm64/publish/baseball-cli
```

## Automated Builds with GitHub Actions

Builds are automatically triggered on:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Creation of tags matching `v*` (e.g., `v1.0.0`)

### Accessing Built Artifacts

**For branch builds:**
1. Go to [Actions](../../actions) tab on GitHub
2. Click the latest workflow run
3. Download artifacts from "Artifacts" section
4. Artifacts are kept for 30 days

**For releases:**
1. Go to [Releases](../../releases) page
2. Find the version you want
3. Download the pre-built executable for your platform:
   - `baseball-cli-windows-x64.exe` - Windows
   - `baseball-cli-linux-x64` - Linux
   - `baseball-cli-macos-x64` - macOS (Intel)
   - `baseball-cli-macos-arm64` - macOS (Apple Silicon)

## Creating a Release

To create a release with pre-built executables:

```bash
# Create an annotated tag (locally)
git tag -a v1.0.0 -m "Release version 1.0.0"

# Push the tag to GitHub
git push origin v1.0.0
```

GitHub Actions will automatically:
1. Build for all platforms
2. Create a release
3. Attach the executables to the release

## Build Options

| Option | Effect | Size | Use Case |
|--------|--------|------|----------|
| `--self-contained` | Includes .NET runtime | 70-100 MB | **Recommended** - no dependencies |
| *(without flag)* | Requires .NET 6 runtime | 10-30 MB | If .NET is already installed |
| `-c Release` | Optimized for production | - | Always use for distribution |
| `-c Debug` | Includes debug symbols | - | Development/debugging only |

## Running the Executable

### Windows
```bash
baseball-cli.exe --help
baseball-cli.exe new               # Create new season
baseball-cli.exe load <season_id>  # Load existing season
```

### Linux/macOS
```bash
./baseball-cli --help
./baseball-cli new
./baseball-cli load <season_id>
```

## Troubleshooting

### Build fails with "dotnet not found"
- Install [.NET 6.0 SDK](https://dotnet.microsoft.com/download)
- Verify installation: `dotnet --version`

### Executable won't run on Linux/macOS
- Make it executable: `chmod +x baseball-cli`
- Run with: `./baseball-cli`

### Permission denied on macOS
```bash
# macOS may block unsigned executables. Allow with:
xattr -d com.apple.quarantine ./baseball-cli
# Then run: ./baseball-cli
```

### Different platform than expected
- Verify your OS: `uname -s` (Linux/macOS) or Windows version
- Download the correct executable for your architecture (x64 vs arm64)

## Supported Platforms

| OS | Architecture | Status |
|----|--------------|--------|
| Windows | x64 | ✅ Supported |
| Windows | ARM64 | Build available on request |
| Linux | x64 | ✅ Supported |
| Linux | ARM64 | Build available on request |
| macOS | x64 (Intel) | ✅ Supported |
| macOS | ARM64 (Apple Silicon) | ✅ Supported |

## Development Builds

For development with live reloading:

```bash
# Run in watch mode (rebuilds on file changes)
dotnet watch run

# Run with debug output
dotnet run -- --debug

# Run tests (when available)
dotnet test
```

## Distributing Your Build

Once built, you can distribute the executable by:

1. **Upload to releases** - Add to GitHub Releases (recommended)
2. **Share as file** - Email, cloud storage, etc.
3. **Web download** - Host on your website
4. **Package managers** - NuGet (for libraries), Chocolatey, Homebrew (advanced)

Users just need to download the executable and run it (Windows) or `chmod +x` + run (Linux/macOS).

## Size Considerations

Self-contained builds are ~70-100 MB due to bundled .NET runtime. This is normal and provides the best user experience.

For smaller deployments, users can install .NET 6 runtime separately and use the framework-dependent build (add `--no-self-contained` to reduce size to ~30 MB).

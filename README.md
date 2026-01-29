# VL Project Template

A template for VL projects, using a batch file opening a specific vvvv and NuGet environment.
Set up for more complex and scalable projects with multiple collaborators.

## Environment

Every project contains a main VL file `NewProject.vl` and the main entry point `NewProject.bat`.

The batch file will set up the environment in vvvv specifically for the project, including the right vvvv version and the NuGet path inside of the project, and eventually open `NewProject.vl`. Therefore it is the file that should be opened everytime one is working on the project. 

Doing so, vvvv will make sure that NuGets are always loaded from the project repository. Loading and storing them within the scope of a project from this folder will make sure that everybody working on the project uses the same NuGets and their versions.

If the specific vvvv version can not be found on your computer, you need to download and install it manually. Feel free to change the vvvv version in the batch file, if the project requires a different version. Just make sure that everybody working on the project is informed about this step and downloads the right version of vvvv.

Additionally, the `vvvv.bat` file can be used to set the environment without opening a default project file.

## Structure

The template also contains some standard folders `assets` `nugets` `rnd` `vl` and an empty placeholder file inside of each if it is empty, so that it gets picked up by Git. The placeholder file can be safely deleted, also the folder itself if it is not to be used within the project.

- `assets` should contain any files like images, videos, fonts that will be picked up and used by the VL application, and are not managed by an external asset provider. You might consider to organize assets by their type, so feel free to create sub folders like `images` `videos` `fonts`.
- `nugets` is the place where all NuGet dependencies will be saved and that is specified as the NuGet override folder in the batch file.
- `rnd` should be used to save research and tests patches that were developed in the scope of a project and/or showcase several possible solutions, but are not part of the project itself. Just make sure that these files are also opened in the same version and that they are using the custom NuGet folder as the rest of the project.
- `vl` contains VL file dependencies that are referenced and used by the project.

### Setting up a new VL project

After creating a new repository based on this template, you need to manually change the name of both the main VL file and the batch file to the name of the project. Also make sure to change the name of the main entry file inside of the batch file!

## Advanced techniques

### VL file dependencies

When working on a project, it might grow and get more complex, so it would make sense to split it into more file dependencies, that are picked up by the project. A file dependency in general is just a VL file containing definitions that can be used in the main project when setting a reference to the dependency.

It is generally good practice to only store definitions like classes, records and process nodes inside file dependencies and not in the main VL file. The main VL file on the other hand does only call these operations on the application side, but does not store any definitions itself. Like that, we ensure modularity and exchangeability, and also open up for other contributors to projects. 

https://thegraybook.vvvv.org/reference/best-practice/version-control.html#version-control-with-git

### Package repositores / Git submodules

In case your project needs to use a VL library from source (often the case when using a fork of VL.Fuse in a project), please add them to a folder called `package-repositories` inside of your project and include it in the batch file. Don’t forget to add the libraries to the `--editable-packages` flag, otherwise they will be pre-compiled in your project. Afterwards use a Git client that supports submodules.

```jsx
taskkill /f /im vvvv.exe
timeout 3
start "" "C:\Program Files\vvvv\vvvv_gamma_7.0-win-x64\vvvv.exe" --nuget-path "%~dp0nugets" --package-repositories "%~dp0package-repositories" --editable-packages VL.Fuse --open "%~dp0NewProject.vl"
exit
```

## Build Automation & Releases

This template includes a build system to compile your VL project into a standalone `.exe` and publish it as a GitHub release. The GitHub Actions workflow automatically downloads and installs vvvv gamma, so no self-hosted runner is required.

### How it works

When you push a git tag like `v1.0.0`, GitHub Actions automatically:

1. Downloads vvvv gamma from the official vvvv TeamCity server
2. Silently installs it on the runner
3. Compiles your project using `vvvvc.exe`
4. Creates a portable `.zip` file containing the standalone application
5. Publishes it as a GitHub release that users can download

### Setup for your project

#### 1. Rename project files

After creating a new repository based on this template, rename the files to match your project:

| Template file | Rename to |
|---------------|-----------|
| `NewProject.vl` | `YourProject.vl` |
| `NewProject.props` | `YourProject.props` |
| `NewProject.bat` | `YourProject.bat` |

Also update the reference inside `YourProject.bat` to point to the renamed `.vl` file.

#### 2. Configure the GitHub workflow

Edit `.github/workflows/release.yml` and update the environment variables at the top:

```yaml
env:
  # Your project name (must match .vl and .props filenames, without extension)
  PROJECT_NAME: YourProject

  # vvvv gamma version - update these when you want to use a different version
  VVVV_VERSION: "7.0"
  VVVV_BUILD_ID: "39385"
```

#### 3. Configure compiler settings (optional)

Edit `YourProject.props` to customize build settings:

- **Application icon**: Uncomment and set `<ApplicationIcon>` to include an `.ico` file
- **Include assets**: Uncomment the `<ItemGroup>` section to bundle the `assets/` folder with your build

### Changing the vvvv gamma version

The workflow downloads vvvv gamma from the official TeamCity build server. To use a different version:

1. Go to [vvvv.org/download](https://vvvv.org/download/)
2. Right-click the download button for the version you want
3. Copy the link - it will look like:
   ```
   https://teamcity.vvvv.org/guestAuth/app/rest/builds/id:39385/artifacts/content/vvvv_gamma_7.0-win-x64_setup.exe
   ```
4. Extract the **build ID** (e.g., `39385`) and **version** (e.g., `7.0`) from the URL
5. Update the workflow environment variables:
   ```yaml
   VVVV_VERSION: "7.0"
   VVVV_BUILD_ID: "39385"
   ```

### Creating a release

```bash
# Commit your changes
git add .
git commit -m "Ready for release"

# Create and push a version tag
git tag v1.0.0
git push origin main --tags
```

The workflow triggers automatically when a tag starting with `v` is pushed. The version number in the tag (e.g., `1.0.0`) is used in the release and zip filename.

You can monitor the build progress in the **Actions** tab of your GitHub repository.

### Local builds

You can also build locally without GitHub Actions:

```bash
# From the project root
nuke/build.cmd Compile --compilerpath "C:\Program Files\vvvv\vvvv_gamma_7.0-win-x64\vvvvc.exe"

# Or specify project name explicitly
nuke/build.cmd Compile --compilerpath "C:\path\to\vvvvc.exe" --projectname "YourProject"
```

**Requirements for local builds:**
- Windows with PowerShell
- .NET 8.0 SDK (auto-downloaded if not present)
- vvvv gamma installed locally

### Build output

After a successful build, you'll find:

```
artifacts/
├── win-x64/
│   └── YourProject/
│       ├── YourProject.exe    # The standalone application
│       └── ...                # Dependencies and resources
└── yourproject_1.0.0_win-x64.zip  # Portable distribution
```

The `.zip` file is what gets uploaded to GitHub releases. Users can download it, extract, and run `YourProject.exe` directly.

### Build system files

| File | Purpose |
|------|---------|
| `Version.props` | Version number (used in zip filename for local builds) |
| `YourProject.props` | vvvv compiler settings (output type, assets, icon) |
| `nuke/build.cmd` | Build entry point (Windows batch file) |
| `nuke/build.ps1` | PowerShell bootstrap (downloads .NET SDK if needed) |
| `nuke/build/_build.csproj` | NUKE build system project |
| `nuke/build/Build.cs` | Build logic (Clean, Compile targets) |
| `.github/workflows/release.yml` | GitHub Actions workflow |

### Troubleshooting

**Build fails with "Could not find .vl file"**
- Ensure `PROJECT_NAME` in the workflow matches your `.vl` filename (without extension)
- Check that both `YourProject.vl` and `YourProject.props` exist

**vvvv installation fails**
- The vvvv build ID may have changed. Get the latest from [vvvv.org/download](https://vvvv.org/download/)
- Check if the TeamCity server is accessible

**Compilation errors**
- Test your project locally in vvvv gamma first
- Ensure all NuGet dependencies are committed in the `nugets/` folder
- Check that `VLIgnoreCompileErrors` is set to `False` in the `.props` file

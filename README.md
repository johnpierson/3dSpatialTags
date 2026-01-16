# 3D Spatial Tags for Revit

⚠️ **USE AT YOUR OWN RISK.** This software is provided "as is," without warranty of any kind, express or implied. The developer is not responsible for any data loss or issues caused by the use of this plugin.

[![Revit 2020](https://img.shields.io/badge/Revit-2020-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![Revit 2021](https://img.shields.io/badge/Revit-2021-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![Revit 2022](https://img.shields.io/badge/Revit-2022-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![Revit 2023](https://img.shields.io/badge/Revit-2023-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![Revit 2024](https://img.shields.io/badge/Revit-2024-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![Revit 2025](https://img.shields.io/badge/Revit-2025-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![Revit 2026](https://img.shields.io/badge/Revit-2026-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

3D Spatial Tags is a Revit add-on that enables the generation of 3D geometry tags for Rooms and Spaces. Since Revit's native tags only exist in 2D views, this tool allows you to visualize room information (Name and Number) directly within 3D views, walkthroughs, and coordination models.

## Key Features

- **3D Room & Space Tags**: Generate 3D model text/geometry based on your room or space parameters.
- **Link Support**: Extract and tag spatial elements from linked Revit models.
- **Phase Awareness**: Filter spatial elements by specific project phases.
- **Dynamic Updates**: Automatically update previously created tags to reflect changes in room names, numbers, or locations.
- **Configurable Text Height**: Adjust the size of the generated 3D tags directly from the UI.
- **Multi-Version Support**: Fully compatible with Autodesk Revit 2020 through 2026.

## How it Works

1. Open the **3D Spatial Tags** panel in your Revit Ribbon.
2. Select your **Target** (Rooms or Spaces).
3. If necessary, enable **From Link** and select the desired Revit Link.
4. Choose the **Phase** you want to process.
5. Choose your preferred **Tag Family**.
6. Click **Create/Update Tags**.

---

<details>
<summary><b>🛠 Technical Details & Build Instructions</b></summary>

### Technologies Used

* C# 12
* .NET Framework 4.8 (for Revit 2020-2024)
* .NET 8 (for Revit 2025-2026)
* WPF / MVVM
* [NUKE Build System](https://nuke.build/)

### Getting Started

Before building, ensure you have the required frameworks installed:
* [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)
* [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet)

### Building the Project

We recommend using **JetBrains Rider** or **Visual Studio 2022**.

#### Using JetBrains Rider (Preferred)
1. Open `ThreeDeeRoomTags.sln`.
2. Select a configuration (e.g., `Release R26` for Revit 2026).
3. Click `Build -> Build Solution`.

#### Using command line (NUKE)
To build all versions and create the MSI installers/bundles:
1. Install NUKE: `dotnet tool install Nuke.GlobalTool --global`
2. Run build: `nuke`
3. Create MSI: `nuke createinstaller`
4. Create Bundle: `nuke createinstaller createbundle`

### Solution structure

| Folder  | Description                                                                |
|---------|----------------------------------------------------------------------------|
| build   | Nuke build system configuration                                            |
| install | WixSharp-based MSI installer projects                                      |
| source  | Plugin source code (ThreeDeeRoomTags)                                      |
| output  | Generated MSI bundles and installer files                                  |

### Project structure (ThreeDeeRoomTags)

| Folder     | Description                                             |
|------------|---------------------------------------------------------|
| Classes    | Global constants and application logic                  |
| Models     | Revit API interaction and data processing               |
| ViewModels | MVVM Logic and UI binding commands                      |
| Views      | XAML-based user interface                               |
| Resources  | Embedded assets and tag families                        |
| Utils      | String parsing and geometry helpers                      |

</details>

## License

This project is licensed under the **GNU General Public License v3 (GPLv3)**. See the [LICENSE](LICENSE) file for details.
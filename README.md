# WasaBii

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Unity Version](https://img.shields.io/badge/Unity-2022.2.6f1%20or%20newer-blue.svg)](https://unity.com/)

## Description

A collection of polished and optimized general purpose utilities used in the [*dProB* software](https://www.bii-gmbh.com/).
Requires only JSON.NET as dependency, which should automatically be included by the Unity package manager.

While this is primarily designed as a Unity package, only the `Unity` and `Extra` packages require Unity as a dependency.

Includes:
- Common types for error handling: `Option<T>` and `Result<T, TError>`
- LINQ-style extensions for collections common in other programming languages
- Unity Coroutine utilities that allow composition in a declarative, functional style
- Customizable unit system to ascribe physical units to numbers
- Extensible geometry wrapper types for typesafe calculations in 3D space
- Standalone Undo System which can be dropped into any type of program
- A customizable spline system including Catmull-Rom splines as well as Bezier splines
- Compiler diagnostics to validate that types are actually immutable when required
- Many other small utilities and minor systems

For a detailed presentation of the available modules and features, you can read [the report](Report.pdf).

## Installation

### Via Git URL

1. Open your Unity project.
2. Open the package manager.
3. Add package from git URL and paste `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii`.

### Manual Installation

1. Download the latest release from this GitHub repository.
2. Unzip the archive.
3. In the Unity Package Manager, use `Add package from disk...`.
4. Select the `package.json` that you want to import.

### Explicitly Specify Subpackages

WasaBii is separated into several dependent sub-packages which can be imported and tracked manually.

- **Core** includes the most central utilities that all other packages depend on
  - `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii/WasaBii-Core`
- **Undo** includes a complete undo system with multiple usage modes and many features that's easy to integrate.
  - `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii/WasaBii-Undo`
- **Units** includes a fully custom and easily customizable system for using physical units instead of random float values
  - `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii/WasaBii-Units`
- **Geometry** builds upon units and provides typesafe wrappers around vectors, with safe calculations and utilities.
  - `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii/WasaBii-Geometry`
- **Splines** provides both bezier curves and catmull rom splines, with customizable coordinate systems and many utilities.
  - `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii/WasaBii-Splines`
- **Unity** provides a ton of utilities that make it much easier to work with the Unity engine.
  - `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii/WasaBii-Unity`
- **Extra** includes rarely used additional utilities and systems. Requires unity.
  - `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii/WasaBii-Extra`

## Contributing

Feel free to open issues and pull requests!

1. Fork the repository.
2. Create your feature branch (`git checkout -b feature/YourFeature`).
3. Commit your changes (`git commit -m 'Add some feature'`).
4. Push to the branch (`git push origin feature/YourFeature`).
5. Open a pull request.

Note that in order to maintain the managed DLLs in WasaBii, you need to have Unity 2022.2.6f1 installed on the C drive via UnityHub.
If you want to use a different unity installation, you need to change the paths in the respective `.csproj` files.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE.md](LICENSE.md) file for details.

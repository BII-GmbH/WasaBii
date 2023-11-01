# WasaBii

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Unity Version](https://img.shields.io/badge/Unity-2022.2.6f1%20or%20newer-blue.svg)](https://unity.com/)

## Description

Short description of what this Unity package does and the problem it solves.

## Features

- Feature 1
- Feature 2
- Feature 3

## Installation

### Via Git URL

1. Open your Unity project.
2. Open the package manager.
3. Add package from git URL and paste `https://github.com/BII-GmbH/WasaBii.git?path=WasaBii-unity-project/Packages/WasaBii`.

### Manual Installation

1. Download the latest release from this GitHub repository.
2. Import the package into your Unity project.

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

## Usage

```csharp
// Sample code to demonstrate how to use WasaBii
```

## Documentation

For more detailed documentation, please refer to [DOCUMENTATION.md](DOCUMENTATION.md).

## Contributing

1. Fork the repository.
2. Create your feature branch (`git checkout -b feature/YourFeature`).
3. Commit your changes (`git commit -m 'Add some feature'`).
4. Push to the branch (`git push origin feature/YourFeature`).
5. Open a pull request.

Note that in order to maintain the managed DLLs in WasaBii, you need to have Unity 2022.2.6f1 installed on the C drive via UnityHub.
If you want to use a different unity installation, you need to change the paths in the respective `.csproj` files.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE.md](LICENSE.md) file for details.

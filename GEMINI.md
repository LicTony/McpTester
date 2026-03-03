# McpTester Project Overview

McpTester is a standard .NET 10.0 WPF (Windows Presentation Foundation) application. It is currently in its initial stages with a boilerplate structure.

## Main Technologies
- **Language:** C#
- **Framework:** .NET 10.0-windows
- **UI Architecture:** WPF (Windows Presentation Foundation) with XAML
- **Development Environment:** Visual Studio / .NET SDK

## Project Structure
- `McpTester.slnx`: Visual Studio solution file.
- `McpTester/`: Main project directory.
    - `McpTester.csproj`: Project configuration file (specifies .NET 10.0, WPF, Nullable, and ImplicitUsings).
    - `App.xaml` / `App.xaml.cs`: Application entry point and global resources.
    - `MainWindow.xaml` / `MainWindow.xaml.cs`: Main UI window and interaction logic.
    - `AssemblyInfo.cs`: Project metadata.

## Building and Running
To build and run the project from the command line:

- **Build:**
  ```powershell
  dotnet build
  ```
- **Run:**
  ```powershell
  dotnet run --project McpTester\McpTester.csproj
  ```
- **Clean:**
  ```powershell
  dotnet clean
  ```

## Development Conventions
- **Naming:** Follows standard .NET naming conventions (PascalCase for classes and methods).
- **Style:** Utilizes modern C# features with `Nullable` and `ImplicitUsings` enabled in the `.csproj`.
- **UI:** Interactive elements are defined in XAML with corresponding logic in code-behind (`.xaml.cs`).

# CodeConverter Repository Summary

## Purpose
This repository contains a code conversion tool that translates between C# and Visual Basic .NET (VB.NET). It provides:
- Command line conversion tools
- Visual Studio extension (VSIX)
- Web interface for code conversion

## General Setup
- Primary solution file: `CodeConverter.sln`
- Targets .NET Framework and .NET Core
- Uses GitHub Actions for CI (.github/workflows/dotnet.yml)
- Contains extensive test suite in `Tests/` directory

## Repository Structure
- `CodeConverter/`: Core conversion logic
- `CodeConv/`: CLI tool for conversion
- `Tests/`: Comprehensive test suite
- `Web/`: Web interface components
- `Vsix/`: Visual Studio extension
- `.github/workflows/`: CI/CD pipelines

## CI/CD & Technologies
- GitHub Actions workflow (`dotnet.yml`) runs:
  - Build and test on Windows/Linux
  - Code analysis
  - Test coverage reporting
- Primary technologies:
  - .NET (Framework and Core)
  - Roslyn compiler
  - Visual Studio SDK (for VSIX)
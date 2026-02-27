# CodeConverter Repository Summary

## Agent instructions

For any bugfix or feature request:
* First: Think carefully about the root cause of the issue, and how it would ideally work for minimal maintenance. Contributing.md has more hints on making good tradeoffs, but always generalise a fix rather than hardcode details of a particular case.
* Use strict TDD.
* The refactor stage should only be what's essential to the task.
* Take inspiration from the current tests and shape of code.
* Once the task is green and complete, make appropriate further refactors to improve code quality and modularity.

## Purpose
This repository contains a code conversion tool that translates between Visual Basic .NET (VB.NET) and C#. It provides:
- Command line conversion tools
- Visual Studio extension (VSIX)
- Web interface for code conversion

## General Setup
- Primary solution file: `CodeConverter.slnx`
- Primarily targets .NET Core
- Uses GitHub Actions for CI which show dependencies required for build and test (.github/workflows/dotnet.yml)
- Contains extensive characterization test suite in `Tests/` directory

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
  - .NET Core
  - Roslyn compiler
  - Visual Studio SDK (for VSIX)
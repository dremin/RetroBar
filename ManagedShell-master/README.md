# ManagedShell
![ManagedShell](https://github.com/cairoshell/ManagedShell/workflows/ManagedShell/badge.svg) [![Nuget](https://img.shields.io/nuget/v/ManagedShell?color=informational)](https://www.nuget.org/packages/ManagedShell/)

A library for creating Windows shell replacements using .NET, written in C#.

## Features
- Tasks service that provides taskbar functionality
- Tray service that provides notification area functionality
- AppBar WPF window class and helper methods
- Several helper classes for common shell functions
- Implements `DependencyProperty`, `INotifyPropertyChanged`, and `ObservableCollection` where appropriate to support binding with WPF
- Supports running as a Windows Explorer replacement, or running alongside Windows Explorer

## Usage
Visit the [ManagedShell Wiki](https://github.com/cairoshell/ManagedShell/wiki) for usage documentation.

## Example implementations
- [RetroBar](https://github.com/dremin/RetroBar)
# Iciclecreek.Avalonia.WindowManager

[![NuGet Version](https://img.shields.io/nuget/v/Iciclecreek.Avalonia.WindowManager.svg)](https://www.nuget.org/packages/Iciclecreek.Avalonia.WindowManager/)
[![Build Status](https://github.com/tomlm/Iciclecreek.Avalonia.WindowManager/actions/workflows/BuildAndRunTests.yml/badge.svg)](https://github.com/tomlm/Iciclecreek.Avalonia.WindowManager/actions/workflows/BuildAndRunTests.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

This library implements a window manager for Avalonia with windows defined using Avalonia instead of native windows.
This gives you the ability to create MDI style user interfaces in Avalonia, even in environments which don't support windowing like Android and iOS.

![windows](https://raw.githubusercontent.com/tomlm/Iciclecreek.Avalonia.WindowManager/refs/heads/main/windows.gif)

# Installation
To install you need to add a reference to the nuget package **Iciclecreek.Avalonia.WindowManager**

```dotnet add package Iciclecreek.Avalonia.WindowManager```

# Usage
This library defines two controls:
* **WindowsPanel** - a panel which hosts windows
* **ManagedWindow** - a Managed Window implementation which is 100% avalonia controls (no system windows).

## WindowsPanel control
The **WindowsPanel** control creates a region that hosts multiple windows. Simply add it to your main view xaml.
```xml
    <wm:WindowsPanel x:Name="Windows"/>
```

## ManagedWindow control
The **ManagedWindow** control is a clone of the **Window** control. It has standard Window properties like **Title**, **WindowState**, **WindowStartupLocation**, **Position**, etc.
Instead of being hosted using Native windows, a ManagedWindow control is hosted via the Avalonia Overlay system.

### Showing a window
To show a window you need to get an instance of the WindowsPanel and call **ShowWindow()**.

For example:
And code behind
```cs
   var window = new ManagedWindow()
   {
       Title = "My window",
       WindowStartupLocation=WindowStartupLocation.CenterScreen,
       Width=300, Height=300
   };
   Windows.Show(window);
```

To close a window you simple call **window.Close()**.

### Showing a Dialog
To show a dialog is exactly the same as Avalonia, you instantiate a ManagedWindow and call **.ShowDialog() **passing in the parent window.
```cs
   var dialogWindow = new ManagedWindow()
   {
       Title = "My window",
       WindowStartupLocation=WindowStartupLocation.CenterScreen,
       Width=300, Height=300
   };
   var result = await dialogWindow.ShowDialog<string>(parent);
```

To close a dialog you call **Close(result)**;

# HotKeys
The window manager supports hotkeys for common actions like closing a window, minimizing, maximizing, etc.

|Hotkey | Action |
|--------|--------|
|Ctrl+F4 | Close the current window |
|Alt+-| Show System menu (Restore/Move/Size/Maximize/Minimize/Close)|
|Ctrl+Tab| Activate the previous window |
|Ctrl+Shift+Tab | Activate the next window |
|Ctrl+F6| Activate the previous window |
|Ctrl+Shift+F6| Activate the next window |

> NOTE: On windows consoles Ctrl+Tab and Ctrl+Shift+Tab are handled by the console window, so Ctrl+F6 and Ctrl+Shift+F6 should be used instead.



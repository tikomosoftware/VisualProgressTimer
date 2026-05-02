# Visual Progress Timer

Visual Progress Timer is a lightweight Windows desktop timer inspired by visual time timers. The remaining time is shown as a colored area on a simple clock face, so you can understand progress at a glance without reading a digital timer.

## Features

- 60-minute visual timer face
- Drag the clock face to set the remaining time
- Mouse wheel fine adjustment
- Start and stop countdown
- Time-up notification and alarm sound
- Color themes for the timer face and frame
- Light and dark mode
- Always-on-top mode
- Floating mode for a minimal timer-only view

## Controls

- Drag the clock face to set time from 1 to 60 minutes.
- Use the mouse wheel to adjust the time by 1 minute.
- Press `Start` to begin or stop the countdown.
- Open `Settings` to change color, dark mode, always-on-top, and floating mode.
- In floating mode, press `Esc` to return to normal mode.
- In floating mode, right-click and drag the timer to move the window.

## Requirements

- Windows
- .NET 10 SDK for development

## Build

```powershell
dotnet build VisualProgressTimer\VisualProgressTimer.csproj
```

## Run

```powershell
dotnet run --project VisualProgressTimer\VisualProgressTimer.csproj
```

## Publish

For a self-contained Windows x64 build:

```powershell
dotnet publish VisualProgressTimer\VisualProgressTimer.csproj -c Release -r win-x64 --self-contained true
```

The published application will be under:

```text
VisualProgressTimer\bin\Release\net10.0-windows\win-x64\publish
```

## Assets

The application icon is stored in:

- `VisualProgressTimer/Assets/AppIcon.ico`
- `VisualProgressTimer/Assets/AppIcon.png`

## License

License not selected yet.

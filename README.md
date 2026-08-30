# NasaDataDashboard

A Blazor Server dashboard for exploring near-Earth asteroid data from NASA's APIs, with orbital positions calculated from scratch and rendered as an interactive 3D scene.

It's a deliberately mixed stack: a C# backend does the actual orbital mechanics, Blazor/MudBlazor handles the dashboard UI, and a Three.js module is injected via JS interop to render the parts a server-rendered UI can't do well — an interactive, clickable 3D view isn't something Blazor draws natively, so this hands that one job to the browser instead of fighting the framework for it.

## What it does

- Pulls near-Earth object data from NASA's NeoWs and SBDB (Small-Body Database) APIs
- Calculates real orbital positions server-side — mean motion via Kepler's third law, Julian date conversions, angle normalization — rather than just relaying raw API fields
- Renders asteroids in an interactive 3D scene (Three.js, via `IJSRuntime`) with orbit controls and a clickable info panel per object
- Dashboard view with charts (bar/line/pie/scatter) summarizing the current asteroid set — distance, velocity, hazard status
- Filtering by distance range

## Stack

- **Backend:** C# / .NET 8, ASP.NET Core Blazor Server
- **UI:** MudBlazor
- **3D rendering:** Three.js (ES module, loaded via `IJSRuntime` interop)
- **Tests:** NUnit
- **Data source:** NASA NeoWs & SBDB APIs

## Status

Uni project but will likely continue to work on this into the future - it would be nice to get more accurate scaling on the render, and create something to test the accuracy of the maths.

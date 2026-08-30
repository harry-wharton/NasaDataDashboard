# NASA Data Dashboard

A Blazor Server dashboard for exploring near-Earth asteroid data, with orbital positions propagated from raw orbital elements — not just pulled from an API — and rendered as an interactive 3D scene.

Yes it is a bit of a messy uni project at the moment but that's what makes it fun... improvements to come lol

It's a deliberately mixed stack: a C# backend does the actual orbital mechanics, Blazor/MudBlazor handles the dashboard UI, and a Three.js module is injected via JS interop for the one job a server-rendered UI can't do well — an interactive, clickable 3D view. Rather than fight the framework for that, it hands it to the browser.

## The maths

Given an asteroid's six orbital elements from JPL's Small-Body Database, `OrbitalCalculator` propagates its position at any point in time:

- **Kepler's third law** derives mean motion (angular speed around the sun) from the semi-major axis alone — `period ∝ a^1.5` — so position can be advanced to *any* timestamp from a single epoch, not just the one returned by the API.
- **Kepler's equation** (`M = E - e·sin(E)`) is solved numerically via Newton-Raphson, seeded with a perturbed guess for faster convergence, iterating to a `1e-12` radian tolerance to get the eccentric anomaly from the mean anomaly.
- Eccentric anomaly is converted to **true anomaly** via `atan2`, then to a heliocentric radius using the orbit equation `r = a(1 - e·cos E)`.
- The resulting 2D orbital-plane position is rotated into 3D ecliptic coordinates using the full three-angle rotation (inclination, longitude of ascending node, argument of perihelion) — the standard classical-orbital-elements-to-Cartesian transform.
- Julian date ⇄ Unix millisecond conversions tie the whole thing to real epochs, and everything runs in `decimal` rather than `double` to avoid compounding floating-point drift across the trig-heavy pipeline.

The output is a live, continuously time-advanceable 3D position for every asteroid — the 3D view isn't animating pre-baked frames, it's evaluating the orbit at the current instant.

## What it does

- Pulls near-Earth object data from NASA's NeoWs API and orbital elements from JPL's SBDB query API, with configurable constraint filtering (e.g. potentially-hazardous-only, max distance) built as JSON `AND` constraints
- In-memory response caching, with cache hits/misses timed and logged separately from live API calls
- Propagates real orbital positions server-side (see above) rather than relaying raw API fields
- Renders asteroids in an interactive 3D scene (Three.js via `IJSRuntime`) with orbit controls and a clickable per-object info panel
- Dashboard charts (bar/line/pie/scatter) summarizing the current asteroid set — distance, velocity, hazard status
- Filtering by distance range
- Unit tests around the API services using mocked `HttpMessageHandler`s, so they run without hitting NASA's servers

## Stack

- **Backend:** C# / .NET 8, ASP.NET Core Blazor Server
- **UI:** MudBlazor
- **3D rendering:** Three.js (ES module, loaded via `IJSRuntime` interop)
- **Tests:** NUnit + Moq
- **Data sources:** NASA NeoWs API, JPL SBDB query API

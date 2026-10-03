# Compendium project instructions

## Catalog JSON guidance

- The Compendium catalog data lives under `src/Compendium/*.json`.
- It is **valid** for different JSON files or different solar-system variants to contain entries with the same celestial name / `Id` string.
- Do **not** rename, delete, or auto-deduplicate those entries just because the same `Id` appears in another catalog file.
- Preserve per-system differences in category membership, descriptive text, and orbit-group metadata when updating catalog entries.
- When making catalog changes, treat the active game's loaded celestial definitions as authoritative; matching the intended in-game `Id` for that system is more important than forcing global uniqueness across all JSON files.
- Sol-system bodies use the `"Compendium"` top-level key. Other star systems have one JSON per system root (e.g. `AlphaCentauri.json`) whose top-level key is the root's `Id`; those keys are checked first for bodies in that system, then `Compendium`. Stars and barycenters of every system go in `Stars.json`.
- Precedence: JSON files inside this mod's own folder are defaults and load first. Entries from any other mod's JSON (tracked in `overrideJsonKeys`) take precedence for the same body or category, whatever top-level key either side uses.

## Game source reference (use instead of ILSpy)

- Decompiled game source lives in `decompiled/` (gitignored): `KSA`, `Brutal.ImGui`, `Brutal.Core.Numerics`, `Planet.Core`. `decompiled/VERSION.txt` records the game version it was made from.
- Search/read those `.cs` files with normal workspace search instead of running ILSpy/ilspycmd for each lookup.
- After a game update (or if `VERSION.txt` doesn't match `KSA.dll`'s version), regenerate with `pwsh ./tools/Refresh-Decompiled.ps1` (add assemblies via `-Assemblies`).
- Game content XML (systems, bodies) is under `C:\Program Files\Kitten Space Agency\Content\Core\`.
- Multi-star-system model: all systems live in one `Universe.CurrentSystem`; each star system is an `IIndependentRoot` in `Universe.Roots` (`FixedStar` or `Barycenter`). `OrbitingStar`/`FixedStar` derive from `StellarBody : Astronomical` (not `Celestial`). `Universe.WorldSun` is the star nearest the camera and changes as it moves - Compendium uses `GetSelectedRoot()` instead.

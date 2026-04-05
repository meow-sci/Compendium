# Compendium project instructions

## Catalog JSON guidance

- The Compendium catalog data lives under `src/Compendium/*.json`.
- It is **valid** for different JSON files or different solar-system variants to contain entries with the same celestial name / `Id` string.
- Do **not** rename, delete, or auto-deduplicate those entries just because the same `Id` appears in another catalog file.
- Preserve per-system differences in category membership, descriptive text, and orbit-group metadata when updating catalog entries.
- When making catalog changes, treat the active game's loaded celestial definitions as authoritative; matching the intended in-game `Id` for that system is more important than forcing global uniqueness across all JSON files.

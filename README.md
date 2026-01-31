# Data System

The Data System provides ScriptableObject-backed data entries with stable IDs, optional localization hooks, and tooling to browse and build libraries of data. It is designed for Unity projects that want a consistent pattern for authoring reusable data assets, indexing them by ID, and exposing them at runtime through lightweight references.

## Installation / Setup

- **Unity version:** 6000.2
- **Package name:** `com.fallingsnowinteractive.datasystem`
- **Dependencies:**
  - `com.fallingsnowinteractive.localization`
  - `com.fallingsnowinteractive.ui`
  - `com.fallingsnowinteractive.general`

Add the package to your project (via your preferred UPM workflow) and ensure the dependencies are available.

## Core Concepts

### `ScriptableData<T>`
Base class for data entries backed by ScriptableObjects. Each entry has a unique ID, localization support via `LocDataProperties`, and optional visuals (e.g., icons, prefabs, or other asset references) to support UI and presentation.

### `Library<TID, TEntry>`
A library asset that stores and indexes entries by ID. It supports a default entry, duplicate ID checking, runtime lookup, and filtering for tooling or UI queries.

### `Instance<TID, TData>`
A lightweight runtime wrapper that references an entry by ID and resolves to the underlying data. This is useful for serialization, save data, and network-friendly references.

### `SerializableDataEntry<T>`
Inline serialized data for lightweight or one-off use cases where creating a ScriptableObject asset is unnecessary.

## Minimal Usage Walkthrough

1. **Create a data type:**
   ```csharp
   public class WeaponData : ScriptableData<string>
   {
       // Add fields for weapon stats, visuals, etc.
   }
   ```
2. **Create a library asset:**
   - In the editor, create a `Library<string, WeaponData>` asset.
   - Add `WeaponData` assets to the library and assign unique IDs.
3. **Retrieve entries at runtime:**
   ```csharp
   WeaponData data = weaponLibrary.GetEntry("sword_001");
   // Or use Instance<string, WeaponData> to reference by ID.
   ```

## Editor Tooling

Editor tools live under `Editor/` and provide:
- **LibraryBuilder UI** for creating and validating libraries.
- **Browser windows** to search, filter, and inspect entries.

Use these tools to curate libraries, resolve duplicate IDs, and confirm metadata before runtime usage.

## API Surface

- `Runtime/ScriptableData.cs`
- `Runtime/Libraries/Library.cs`
- `Runtime/Instance.cs`
- `Runtime/LocDataProperties.cs`


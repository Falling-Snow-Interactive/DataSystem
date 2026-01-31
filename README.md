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

## Library Browser

The Library Browser is an `EditorWindow` (`LibraryBrowserWindow`) that builds a multi-column table from the serialized properties of the first entry in the selected library. Each column maps to a visible serialized field, with a leading “open” column that opens the entry asset in a property editor. Columns are rebuilt whenever you switch libraries or refresh, and the browser skips `m_Script`, transient properties, non-serialized fields, and any field marked with `HideInBrowserAttribute`.【F:Editor/Libraries/Browsers/LibraryBrowserWindow.cs†L18-L1010】

### Opening the browser

There is no default menu item in this package. To open the browser, create your own editor window class that derives from `LibraryBrowserWindow`, provides library descriptors/mappings, and adds a `[MenuItem]` to show it (or call `ShowWindow()` from your own tooling). Once open, use the Library dropdown to switch libraries and click the open icon to inspect an entry.【F:Editor/Libraries/Browsers/LibraryBrowserWindow.cs†L58-L318】

### Attribute behaviors

- **`LibraryAttribute`**: Apply this to a `ScriptableData` (or other library entry) reference field to use the custom library selector in the inspector. The drawer renders a dropdown of library entries with quick buttons to ping or open the selected asset.【F:Runtime/Libraries/LibraryAttribute.cs†L7-L13】【F:Editor/Libraries/LibraryAttributeDrawer.cs†L11-L123】
- **`BrowserPropertyAttribute`**: Apply this to a serialized field to override the browser column display name, hide the column, or show the field in a popup column via named parameters such as `DisplayName`, `HideInBrowser`, and `Popup`.【F:Runtime/Libraries/Browsers/BrowserPropertyAttribute.cs†L1-L34】
- **`HideInBrowserAttribute`**: Apply this to a serialized field to omit it from the browser’s columns.【F:Runtime/Libraries/Browsers/HideInBrowserAttribute.cs†L7-L15】【F:Editor/Libraries/Browsers/LibraryBrowserWindow.cs†L371-L411】
- **`BrowserPopupAttribute`**: Apply this to a serialized class field to replace its column with an “Open” button that launches a popup inspector for that field, keeping the browser table tidy.【F:Runtime/Libraries/Browsers/BrowserPopupAttribute.cs†L7-L15】【F:Editor/Libraries/Browsers/LibraryBrowserWindow.cs†L471-L535】【F:Editor/Libraries/Browsers/SerializedClassPopupWindow.cs†L7-L83】

> Migration note: `BrowserPropertyAttribute` is a superset of the older `HideInBrowserAttribute` and `BrowserPopupAttribute`. Existing usages remain supported, and you can adopt the new attribute incrementally as needed.【F:Runtime/Libraries/Browsers/BrowserPropertyAttribute.cs†L1-L34】【F:Editor/Libraries/Browsers/LibraryBrowserWindow.cs†L520-L636】

```csharp
using UnityEngine;
using Fsi.DataSystem;
using Fsi.DataSystem.Libraries;
using Fsi.DataSystem.Libraries.Browsers;

public class WeaponData : ScriptableData<string>
{
    [HideInBrowser] [SerializeField] private string internalNotes;
    [BrowserPopup] [SerializeField] private WeaponTuning tuning;
}

public class Loadout : MonoBehaviour
{
    [Library] [SerializeField] private WeaponData primaryWeapon;
}
```

## API Surface

- `Runtime/ScriptableData.cs`
- `Runtime/Libraries/Library.cs`
- `Runtime/Instance.cs`
- `Runtime/LocDataProperties.cs`

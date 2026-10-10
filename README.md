# Sprocket JSON Editor

Reusable in-game catalogue editor for Sprocket mods. **[Download 0.1.0](https://github.com/RoanWassink/SprocketJsonEditor/releases/tag/v0.1.0)**. Included in the Cold War Expansion Pack 0.3.0.

## What it provides

Expandable entries, add/duplicate controls, text and numeric inputs, provider-defined dropdowns, validation feedback, scrolling and Save and refresh. The editor blocks gameplay input while open. Providers determine their fields, supported ranges, file paths, backup and runtime-refresh behaviour; the framework does not invent gameplay properties.

## Install and use

Requires Sprocket 0.2.55.5 and the working BepInEx 6 IL2CPP mod loader. Close the game and merge BepInEx into the folder containing Sprocket.exe. Keep one SprocketJsonEditor.dll. Install a compatible consumer such as Shell Selector 0.13.0 or Material Selector 0.5.0; this library has no standalone catalogue button. Open the editor from that mod's part panel. Changes affect every part using the shared entry.

## For mod authors

Reference SprocketJsonEditor.dll without copying it privately into your plugin folder. Add a BepInEx dependency on sprocket.jsoneditor. Subclass the public EditorSession API: supply Items and Templates, Fields, Value, Caption, Set, Duplicate and Save. Choices and ChoiceLabel provide dropdown options; SetChoice can map UI choices to stored data. Call JsonEditor.Open(session) on the game main thread, and close the active editor before opening another. JsonEditor.IsOpen and Close expose the editor lifetime.

Groups, FieldGroup and GroupDescription organize module sections. NumberRange returns EditorNumberRange (minimum, maximum, step) for supported numeric bounds and sliders. JsonFileTransaction.Commit supports conflict checks, backups and rollback for a group of text-file changes.

Your Save implementation owns schema validation, safe writes/backups and in-game refresh. The UI calls Save only after visible field errors are resolved. Keep stable entry IDs and preserve unrelated custom content. See EditorSession.cs and the Shell/Material consumers for complete working implementations; the framework is not an automatic loader for arbitrary JSON schemas.

## Troubleshooting and removal

If a consumer's editor button is absent, check its dependencies and BepInEx log. A Save failure may occur in the consumer's persistence or refresh callback; retain its backup and follow that mod's recovery instructions. Close the editor before changing scenes. Do not remove this DLL while a dependent plugin is installed; roll back the dependency and consumers together if necessary. No game assemblies or loader are bundled.

MIT licensed; created with AI assistance. [Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods.](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6)

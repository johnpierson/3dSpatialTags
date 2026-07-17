## Why

The current tagging workflow can update the wrong tag when the same linked model is placed more than once, create hidden duplicates when an existing tag is not editable, and operate on stale or invalid UI selections. These failures can silently produce incorrect Revit model data, so the workflow needs stronger source identity, validation, and user-visible failure handling across Revit 2020-2026.

## What Changes

- Identify a linked spatial element by its source element, source document, and specific link instance so repeated placements do not overwrite one another.
- Preserve existing host-tag update behavior while migrating legacy tags only when their source can be resolved unambiguously.
- Skip and report existing tags that cannot be edited instead of creating overlapping replacements and suppressing the warning.
- Keep target type, link mode, selected link, phase, and collected spatial elements synchronized whenever the user changes a selection.
- Exclude unloaded links from tagging choices and handle a link becoming unavailable without crashing.
- Validate the selected tag family and all required parameters before starting mutations; report incompatible families and invalid text heights to the user.
- Gate the Create/Update command on a complete, valid selection rather than fragile index and XAML bindings.
- Add automated coverage for deterministic workflow logic and define manual Revit verification for host models, transformed links, repeated link instances, worksharing, and invalid family data.
- Remove obsolete package references where unused and pin compatible dependency versions so supported Revit builds are reproducible and free of avoidable compatibility warnings.

This change affects every supported configuration from Revit 2020 through Revit 2026. It does not change the visual design of the dialog, add support for non-point spatial element locations, delete existing duplicate tags automatically, or narrow the supported Revit-version range.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `spatial-tagging`: Strengthen source identity, update behavior, selection synchronization, family validation, command availability, error reporting, and linked-model availability requirements.

## Impact

- Primary code impact: `ThreeDeeRoomTagModel`, `ThreeDeeRoomTagViewModel`, `ThreeDeeRoomTagView`, tag-family parameter handling, and failure preprocessing.
- The bundled `3dSpatialElementTag.rfa` family and versioned family assets may require new source-identity parameters; installers must continue packaging the compatible family resource.
- Existing tags containing only `SpatialElementId` remain supported through a conservative, unambiguous migration path. Ambiguous legacy tags are reported and left unchanged.
- Revit document changes remain transaction-bound; discovery and validation occur before mutation, with text-height and tag changes coordinated to avoid partial success.
- Project dependency declarations and build verification change, but no public API or external service is introduced.

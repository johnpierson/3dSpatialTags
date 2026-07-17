## Context

Tag discovery, UI selection state, family validation, matching, and Revit document mutation are currently interleaved. Existing tags store only `SpatialElementId`, which identifies an element inside its source document but not the link instance through which it is tagged. The dialog also persists collection indexes and uses code-behind events to refresh dependent selections, allowing the visible selection and the collected element set to diverge.

The change must work on the Revit 2020-2024 .NET Framework 4.8 targets and the Revit 2025-2026 .NET 8 Windows targets. Revit API objects remain UI-thread-bound, and every family load, type parameter change, and family-instance mutation must occur within a valid transaction.

## Goals / Non-Goals

**Goals:**

- Give every host or linked spatial element a deterministic source identity that distinguishes repeated instances of the same link.
- Validate the complete operation before mutation and produce explicit created, updated, skipped, and failed outcomes.
- Keep dialog state internally consistent and make command availability reflect whether an operation can run.
- Preserve compatible legacy tags and upgrade them only when their identity is unambiguous.
- Keep host/link coordinate conversion explicit and testable.
- Restore clean, reproducible builds for representative legacy and current Revit targets.

**Non-Goals:**

- Automatically delete or merge duplicate tags already present in a model.
- Support area-based or boundary-derived placement when a spatial element has no `LocationPoint`.
- Redesign the WPF dialog or add background/asynchronous Revit document access.
- Change installer technology or remove support for any Revit version from 2020 through 2026.
- Infer a legacy tag's link source when multiple sources are plausible.

## Decisions

### Store composite source identity on every tag

Keep `SpatialElementId` for backward compatibility and add text parameters for `SourceDocumentId` and `SourceLinkInstanceId`. `SourceDocumentId` uses a stable document-level identifier available in every supported Revit version, preferring the worksharing central GUID where applicable and otherwise using a stable model identifier such as the project-information unique ID. Host tags use the active document ID and an empty link-instance ID; linked tags use the linked document ID and the selected `RevitLinkInstance.UniqueId`.

The update key is `(SourceDocumentId, SourceLinkInstanceId, SpatialElementId)`. This separates two placements of the same linked model while retaining deterministic host updates. Existing tags with only `SpatialElementId` are eligible for in-place migration only when exactly one selected source can own that ID and exactly one legacy tag matches. Ambiguous tags are skipped and reported.

Rejected alternatives:

- `SpatialElementId` alone cannot distinguish documents or repeated link instances.
- Link name or file path is mutable and is not unique when the same model is placed repeatedly.
- Encoding all identity into the existing parameter would break existing integrations and make migration opaque.

### Build an operation plan before opening a mutation transaction

Discovery creates typed operation records containing source identity, transformed host point, desired values, matching tag, editability, and outcome. Required family parameters are validated for presence, storage type, and writability before placement starts. Existing tags are indexed by composite key rather than scanned repeatedly.

For linked elements, the source point is transformed exactly once with the selected link instance's transform while planning. Host points are left unchanged. An unloaded or subsequently unavailable link invalidates the plan and is reported without opening a transaction.

The run uses a `TransactionGroup` containing narrowly named transactions for any family-type height change and tag mutations. The group is assimilated only after required mutations succeed. Non-editable or ambiguous individual tags are planned as skips and do not cause a duplicate to be created. Unexpected Revit failures roll back the active transaction/group and are surfaced to the user. The broad duplicate-instance warning suppression is removed because duplicates are no longer an accepted fallback.

Rejected alternatives:

- One transaction per spatial element would allow partial results and add Revit transaction overhead.
- Continuing to create a replacement for a non-editable tag preserves apparent throughput but corrupts model intent.
- Catching parameter exceptions and reporting success would retain the current partial-success ambiguity.

### Make selection state view-model-owned and object-based

The view model owns `SelectedTarget`, `FromLink`, `SelectedLink`, `SelectedPhase`, and `SelectedFamilySymbol`. A change to any upstream selection clears dependent data and recollects only when the required inputs are present. The view binds selected objects rather than persisted collection indexes; saved preferences are resolved against current collections and ignored when unavailable.

`RunCommand.CanExecute` is derived from a compatible family, selected phase, non-empty taggable elements, and—when linked mode is active—a loaded selected link. A separate `HasLoadedLinks` Boolean controls link-mode availability. Code-behind is limited to view-only behavior and no longer coordinates domain state.

Rejected alternatives:

- Repairing individual XAML bindings would not prevent stale collections created by event ordering.
- Retaining index-based selection makes settings invalid whenever collection ordering or available family types change.

### Treat the bundled family schema as a versioned compatibility contract

The bundled and versioned `3dSpatialElementTag` family assets gain the two source-identity text parameters. Family symbols are considered compatible only when `Name`, `Number`, `SpatialElementId`, `SourceDocumentId`, `SourceLinkInstanceId`, and `Text Height` meet their required storage and mutability rules. Incompatible custom families remain untouched and are reported; the add-in does not silently rewrite user-authored families.

The embedded family remains the installation fallback. Installer packaging is verified after the resource update. A schema/version marker may be added if implementation shows parameter inspection alone cannot distinguish revisions consistently across supported Revit releases.

### Separate deterministic logic for automated tests

Source-key construction, legacy-match resolution, selection-state transitions, parameter-validation results, and operation-summary formatting are extracted from direct Revit mutations where practical. These units receive automated coverage on both target-framework families. Revit integration behavior is still manually verified using `_testFiles/main.rvt`, `_testFiles/link.rvt`, and `_testFiles/link2.rvt`, including a transformed link and two instances of the same link.

Dependency versions are pinned after confirming the version compatible with each Revit target. The unused `Microsoft.Net.Http` reference is removed; `PresentationFramework.Aero2` is retained only if visual verification proves it is required and compatible.

## Risks / Trade-offs

- [Existing family assets lack the new parameters] -> Update every shipped family source, embed the upgraded resource, and verify installer contents before release.
- [A stable document identifier behaves differently across Revit versions or detached models] -> Centralize identifier selection behind one adapter and manually test workshared, non-workshared, detached, and linked fixtures on representative legacy/current versions.
- [Legacy tags cannot always be attributed safely] -> Migrate only one-to-one unambiguous matches and report the remainder without mutation.
- [A link is unloaded after the dialog opens] -> Revalidate the selected link immediately before planning and disable or abort the command with a clear message.
- [Transaction-group rollback removes otherwise successful tag changes] -> Prefer atomic model consistency; report the failure so the user can correct the family or ownership issue and retry.
- [Pinned Nice3point packages differ by Revit version] -> Pin per supported Revit version/configuration rather than forcing a single package version across incompatible APIs.

## Migration Plan

1. Update and verify all bundled/versioned family assets with the composite identity parameters.
2. Ship code that reads both the new composite identity and the legacy `SpatialElementId` representation.
3. On update, write the composite identity to newly created tags and to legacy tags only after an unambiguous match.
4. Leave ambiguous and non-editable legacy tags unchanged and include them in the operation summary.
5. Validate `Debug R24` and `Debug R26`, then exercise the manual host/link/worksharing matrix before packaging.
6. Roll back by restoring the prior add-in and family resource; newly written text parameters remain harmless to the prior version, although it will fall back to legacy matching behavior.

## Open Questions

- Confirm the exact shared/family parameter GUIDs and whether existing released families already reserve suitable identity parameters.
- Confirm which stable non-workshared document identifier is preserved by the supported Revit releases and by the project's typical copy/detach workflows.
- Decide whether skipped ownership conflicts should be shown only in the completion summary or also selected/highlighted in Revit.

> **Status.** Most of this change has shipped. Some of it shipped in PR #5 (commit `1877828`)
> outside the OpenSpec process, and the rest in the remediation that followed an audit of the
> repository. The requirements that now describe current behavior have been merged into
> `openspec/specs/spatial-tagging/spec.md`; what is left unchecked below is genuinely
> unimplemented.
>
> Three decisions differ from the design and are worth reading before picking this up:
>
> - **Source identity is written twice** (task 2.1). The upgraded families carry
>   `SourceDocumentId` and `SourceLinkInstanceId`, which is what Revit can schedule and filter
>   on and what matching prefers to read. The identity is also composed into the existing
>   `SpatialElementId` text parameter, because that is what every tag placed by an earlier
>   version already carries and what a family without the new parameters still has. A family
>   missing them is not rejected — it works, it just cannot be scheduled by source.
> - **Supported Revit versions were narrowed to 2025–2027** rather than upgrading families for
>   2020–2024. Revit families load forward but never backward, so the single embedded family
>   could only serve every version by being saved in the oldest of them; the upgraded family is
>   saved in 2025. Each configuration now embeds its own year's family from `revit/<year>/`,
>   which is also why this cannot recur when 2028 arrives. This is a breaking change and the
>   reason for the 2.0.0 version.
> - **The duplicate-instance warning suppression was narrowed, not removed** (task 3.4). It now
>   silences only warnings whose every element is a tag the run itself created or updated,
>   which is the tool arguing with itself after "place a fresh set every time". Removing it
>   entirely would make that documented mode noisy for no gain.
>
> Task 4.2 is genuinely unimplemented: selection state is still held as combo-box indices
> rather than as the selected objects, and there are no state-transition tests. Everything else
> left open is the manual Revit verification in section 6, which needs a Revit session. Note
> that 6.4 and 6.5 mention Revit 2024, which is no longer supported — read those as 2025.

## 1. Deterministic Workflow Foundations

- [x] 1.1 Add a test project that can exercise framework-neutral tagging workflow logic for the .NET Framework 4.8 and .NET 8 Windows target families, and verify the empty suite in `Debug R24` and `Debug R26`.
- [x] 1.2 Implement the composite source identity value type and document-identity adapter, with tests for host elements, transformed linked elements, and two instances of the same linked document.
- [x] 1.3 Implement typed family-parameter validation and operation-result models, with tests for missing parameters, wrong storage types, read-only parameters, invalid height values, and outcome aggregation.

## 2. Family And Link Compatibility

- [x] 2.1 Add `SourceDocumentId` and `SourceLinkInstanceId` text parameters to the embedded `source/ThreeDeeRoomTags/Resources/3dSpatialElementTag.rfa` and each supported source family under `revit/`, preserving the legacy `SpatialElementId` parameter.
- [x] 2.2 Update family discovery to offer only compatible symbols, load the upgraded bundled family when required, and return an actionable compatibility result instead of dereferencing missing parameters.
- [x] 2.3 Filter unavailable link instances during discovery and revalidate `GetLinkDocument()` immediately before collection and operation planning.
- [x] 2.4 Verify the upgraded family resource is embedded in `Debug R24` and `Debug R26` outputs and included by the existing installer/bundle pipeline.

## 3. Tag Planning And Transactions

- [x] 3.1 Build an operation planner that validates source data, applies the selected link transform exactly once, and classifies each element as create, update, or skip before opening a mutation transaction; cover host and linked cases with tests.
- [x] 3.2 Index existing tags by composite source identity and implement conservative legacy matching, with tests for one-to-one migration, repeated link instances, duplicate legacy tags, and ambiguous document sources.
- [x] 3.3 Implement coordinated text-height and tag mutations with explicit transaction/transaction-group rollback, and verify an unexpected failure cannot produce a success result or retain partial changes.
- [x] 3.4 Change non-editable matches to skipped ownership/model-update outcomes, remove the duplicate-creation fallback and broad duplicate warning suppression, and test that no replacement operation is planned.
- [x] 3.5 Persist composite identity on every created or unambiguously migrated tag and report created, updated, skipped, and failed counts with category-specific reasons.

## 4. View-Model State And Command Safety

- [x] 4.1 Replace collection-index workflow state with selected target, link, phase, and family objects while resolving existing saved preferences only when they match an available object.
- [ ] 4.2 Centralize dependent clearing and recollection in the view model so target, link mode, selected link, and phase changes cannot leave stale spatial elements; add state-transition tests for each selection sequence.
- [x] 4.3 Derive `RunCommand.CanExecute` and `HasLoadedLinks` from valid workflow state, replace the invalid `Rooms.Count` and integer-to-Boolean bindings, and reduce code-behind to view-only behavior.
- [x] 4.4 Surface family, link, height, ownership, ambiguity, and rollback results in the dialog without broad exception swallowing, then verify command enablement and messages through WPF-level tests where practical.

## 5. Dependencies And Automated Verification

- [x] 5.1 Remove the unused `Microsoft.Net.Http` reference, evaluate whether `PresentationFramework.Aero2` remains necessary, and pin compatible Nice3point and application package versions per supported Revit configuration.
- [x] 5.2 Run the automated tests and compile `Debug R24`, confirming zero errors and documenting any unavoidable warnings for the .NET Framework/Revit 2024 configuration.
- [x] 5.3 Run the automated tests and compile `Debug R26`, confirming zero errors and documenting any unavoidable warnings for the .NET 8/Revit 2026 configuration.

## 6. Manual Revit Verification

- [ ] 6.1 Using `_testFiles/main.rvt`, verify Room and MEP Space selection changes refresh the correct host elements, invalid selections disable the command, valid tags create/update, and invalid family or height data is reported without mutation.
- [ ] 6.2 Using `_testFiles/main.rvt`, `_testFiles/link.rvt`, and `_testFiles/link2.rvt`, verify transformed coordinates, unloaded-link handling, and independent create/update behavior for two instances of the same linked document.
- [ ] 6.3 In a workshared copy of `_testFiles/main.rvt`, verify a tag owned by another user is skipped and reported without creating a duplicate or suppressing a duplicate warning.
- [ ] 6.4 Verify one unambiguous legacy tag is migrated, ambiguous legacy tags remain unchanged and reported, and the upgraded bundled family loads correctly in representative Revit 2024 and Revit 2026 sessions.
- [ ] 6.5 Build the release installer/bundle, install it for a representative legacy and current Revit version, and confirm the manifest, assemblies, and upgraded family resource are packaged and load successfully.

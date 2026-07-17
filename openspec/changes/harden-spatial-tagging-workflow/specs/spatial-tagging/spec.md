## MODIFIED Requirements

### Requirement: Spatial element selection

The add-in SHALL allow a user to select either Rooms or MEP Spaces from a selected project phase, SHALL keep collected elements synchronized with the current target, source, link, and phase selections, and SHALL discard saved selections that are unavailable in the current document.

#### Scenario: Select host elements by phase

- **WHEN** a user selects a target type and a phase in the active document
- **THEN** the add-in lists spatial elements of that type whose phase matches the selected phase

#### Scenario: Change the target type

- **WHEN** a user changes the target from Rooms to MEP Spaces or from MEP Spaces to Rooms
- **THEN** the add-in clears the previously collected elements and refreshes them only for the new target and current valid source selections

#### Scenario: Restore an unavailable saved selection

- **WHEN** a saved family, link, or collection index does not resolve to an available object in the current document
- **THEN** the add-in leaves that selection unset and does not run with a different object at the saved index

### Requirement: Linked model support

The add-in SHALL allow spatial elements to be sourced only from a loaded Revit link, SHALL place their tags at the corresponding host-document coordinates, and SHALL distinguish separate instances of the same linked document when creating or updating tags.

#### Scenario: Tag an element from a transformed link

- **WHEN** a user selects a loaded link whose transform is not identity and creates tags
- **THEN** each created tag is positioned using the selected link instance transform applied once to the linked spatial element location

#### Scenario: Tag two instances of the same linked model

- **WHEN** the same linked document is placed through two link instances and the same source spatial element is tagged from each instance
- **THEN** the add-in creates or updates one distinct host tag for each link instance without moving or replacing the other instance's tag

#### Scenario: Encounter an unloaded link

- **WHEN** a link is unloaded or unavailable before collection or immediately before tag creation
- **THEN** the add-in does not query or mutate that linked document and reports that the link must be loaded

### Requirement: Tag family availability

The add-in SHALL make the bundled `3dSpatialElementTag` family available when no compatible family is loaded and SHALL accept a family symbol for tagging only when all required value, identity, and text-height parameters have compatible storage and access.

#### Scenario: Load the bundled family

- **WHEN** the tag dialog opens and no compatible `3dSpatialElementTag` family is loaded
- **THEN** the add-in loads the installed or embedded family resource containing the required parameters and offers its symbols for selection

#### Scenario: Reject an incompatible family

- **WHEN** a candidate family is missing Name, Number, SpatialElementId, SourceDocumentId, SourceLinkInstanceId, or Text Height, or a required parameter has an incompatible storage type
- **THEN** the add-in does not use that family for tag mutations and reports the compatibility problem

### Requirement: Tag creation

The add-in SHALL create a non-structural family instance for each taggable spatial element, populate its Name and Number, and store the source element, source document, and link-instance identity required for later updates.

#### Scenario: Create a tag for a valid host room

- **WHEN** a selected host room has a point location, non-empty name and number, and no matching tag
- **THEN** the add-in creates a tag at that location with matching values, the room unique identifier, the host document identifier, and an empty link-instance identifier

#### Scenario: Create a tag for a valid linked space

- **WHEN** a selected linked MEP Space is taggable and has no matching tag for the selected link instance
- **THEN** the add-in creates a tag at its transformed host point and stores the linked document, link instance, and spatial element identifiers

#### Scenario: Skip an invalid spatial element

- **WHEN** a selected spatial element has no point location or has an empty name or number
- **THEN** the add-in does not create a tag for that element and includes it in the skipped result count

### Requirement: Existing tag updates

The add-in SHALL update only a matching editable tag when update mode is enabled, SHALL NOT create a replacement when the match is non-editable or ambiguous, and SHALL migrate a legacy identifier only when the source match is unambiguous.

#### Scenario: Update an existing host tag

- **WHEN** update mode is enabled and an editable tag's source document, empty link-instance identifier, and SpatialElementId match a selected host spatial element
- **THEN** the add-in updates the tag type, position, name, and number without creating another tag

#### Scenario: Update a tag from one repeated link instance

- **WHEN** update mode is enabled and a tag's source document, link-instance identifier, and SpatialElementId match a selected linked spatial element
- **THEN** the add-in updates that tag without modifying the tag belonging to another instance of the same link

#### Scenario: Existing tag is owned by another user

- **WHEN** a matching tag is not editable because of worksharing ownership or model-update state
- **THEN** the add-in leaves that tag unchanged, does not create an overlapping replacement, and reports the skipped ownership conflict

#### Scenario: Migrate an unambiguous legacy tag

- **WHEN** exactly one legacy tag contains a matching SpatialElementId and exactly one current source can own that identifier
- **THEN** the add-in updates the tag and writes its source document and link-instance identity

#### Scenario: Preserve an ambiguous legacy tag

- **WHEN** a legacy SpatialElementId could match more than one source or more than one legacy tag
- **THEN** the add-in leaves the legacy tags unchanged, creates no replacement, and reports the ambiguity

### Requirement: Configurable text height

The add-in SHALL apply and retain a valid text height for the selected compatible family type and SHALL reject an invalid or unwritable height without reporting it as successfully applied.

#### Scenario: Apply a valid height

- **WHEN** a user enters a valid feet-and-inches value and runs tag creation with a writable Text Height parameter
- **THEN** the selected family type's Text Height is updated within the coordinated mutation and the entered value is saved in user settings

#### Scenario: Reject an invalid height

- **WHEN** the entered text height cannot be parsed as a positive supported model-unit value
- **THEN** the add-in does not save or apply the value and reports the validation error

## ADDED Requirements

### Requirement: Tagging command readiness

The add-in SHALL enable the Create/Update command only when the current selections form a valid tagging operation.

#### Scenario: Complete host selection

- **WHEN** a compatible family, target type, phase, and at least one taggable host spatial element are selected
- **THEN** the Create/Update command is enabled

#### Scenario: Incomplete linked selection

- **WHEN** linked mode is selected without a loaded selected link or without collected taggable elements
- **THEN** the Create/Update command is disabled

### Requirement: Tagging operation result

The add-in SHALL report created, updated, skipped, and failed outcomes without presenting a rolled-back or rejected mutation as successful.

#### Scenario: Complete with skipped elements

- **WHEN** valid tags are created or updated while other elements are skipped for invalid data, ownership, or ambiguity
- **THEN** the completion result reports each outcome count and provides a reason for each skipped category

#### Scenario: Unexpected mutation failure

- **WHEN** an unexpected Revit failure prevents the coordinated mutation from completing
- **THEN** the add-in rolls back the coordinated changes and reports the failure instead of a success count

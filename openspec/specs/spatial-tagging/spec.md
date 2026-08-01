# Spatial Tagging

## Purpose

Define the user-visible behavior for creating and updating 3D tags for Rooms and MEP Spaces in host and linked Revit documents.

## Requirements

### Requirement: Spatial element selection

The add-in SHALL allow a user to select either Rooms or MEP Spaces from a selected project phase.

#### Scenario: Select host elements by phase

- **WHEN** a user selects a target type and a phase in the active document
- **THEN** the add-in lists spatial elements of that type whose phase matches the selected phase

### Requirement: Linked model support

The add-in SHALL allow spatial elements to be sourced from a loaded Revit link and SHALL place their tags at the corresponding host-document coordinates.

#### Scenario: Tag an element from a transformed link

- **WHEN** a user selects a loaded link whose transform is not identity and creates tags
- **THEN** each created tag is positioned using the link instance's total transform applied to the linked spatial element location

#### Scenario: Offer only links that can be read

- **WHEN** the dialog lists the links in the active document
- **THEN** links that report no document, such as unloaded ones, are not offered

#### Scenario: Select the link picked in the model

- **WHEN** a single link instance is selected in the model as the dialog opens
- **THEN** that link instance is chosen, matched by id rather than by name, and the phases offered are the link's own

### Requirement: Source identity

The add-in SHALL identify a tag's source by both the spatial element and the link instance it was read through, so that repeated placements of one linked document do not share tags.

#### Scenario: Tag the same element through two placements of one link

- **WHEN** a linked document is placed twice and a user tags each placement in turn
- **THEN** each placement receives and keeps its own tags, and tagging one does not move or re-point the other's

#### Scenario: Adopt a tag written before source identity existed

- **WHEN** a tag carries only a bare spatial element id and a user tags a link containing that element
- **THEN** the add-in updates that tag rather than placing a duplicate, records the full source identity on it, and reports how many such tags were adopted

#### Scenario: Do not adopt one legacy tag twice

- **WHEN** two placements of one linked document are tagged and only one legacy tag exists for an element
- **THEN** exactly one placement adopts it and the other receives a new tag

### Requirement: Tag family availability

The add-in SHALL make the bundled `3dSpatialElementTag` family available when no compatible family is loaded in the active document.

#### Scenario: Load the bundled family

- **WHEN** the tag dialog opens and no family containing `3dSpatialElementTag` is loaded
- **THEN** the add-in loads the installed or embedded family resource and offers its symbols for selection

### Requirement: Tag creation

The add-in SHALL create a non-structural family instance for each taggable spatial element and populate its Name, Number, and SpatialElementId parameters.

#### Scenario: Create a tag for a valid room

- **WHEN** a selected room has a point location and non-empty name and number values
- **THEN** the add-in creates a tag at that location with values matching the room and stores the room unique identifier

#### Scenario: Skip an invalid spatial element

- **WHEN** a selected spatial element has no point location or has an empty name or number
- **THEN** the add-in does not create a tag for that element

### Requirement: Existing tag updates

The add-in SHALL update a matching editable tag instead of creating a duplicate when update mode is enabled.

#### Scenario: Update an existing tag

- **WHEN** update mode is enabled and an editable tag records the same source identity as the selected spatial element
- **THEN** the add-in updates the tag type, position, name, and number without creating another tag

#### Scenario: Leave a tag owned by another user alone

- **WHEN** a matching tag is owned by another user in a workshared model, or has been changed or deleted in central
- **THEN** the add-in leaves that tag untouched, creates no replacement for it, and reports how many were skipped

#### Scenario: Report tags whose element is gone

- **WHEN** the document contains tags recording source elements that no longer exist in the document being read
- **THEN** the add-in reports how many and leaves them in place

### Requirement: Duplicate warning handling

The add-in SHALL suppress duplicate-instance warnings only for tags it placed on top of its own tags in the same run.

#### Scenario: Place a fresh set over an existing one

- **WHEN** update mode is disabled and a run places tags where its own previous tags already are
- **THEN** the resulting duplicate-instance warnings are suppressed

#### Scenario: Preserve an unrelated warning

- **WHEN** a duplicate-instance warning names any element the run did not create or update
- **THEN** that warning is not suppressed

### Requirement: Configurable text height

The add-in SHALL allow a valid text height to be applied to the selected tag family type and retained for a later session.

#### Scenario: Apply a valid height

- **WHEN** a user enters a valid feet-and-inches value and runs tag creation
- **THEN** the selected family type's Text Height is updated and the entered value is saved in user settings

#### Scenario: Read a height the same way everywhere

- **WHEN** a user enters a decimal feet value such as `1.5'` on any system locale
- **THEN** the value is read identically regardless of that locale's decimal and grouping separators

#### Scenario: Report a height that cannot be read

- **WHEN** the entered value is not a length the add-in can read
- **THEN** the Settings tab reports it, the value is not saved, and the family type's own height is left alone

### Requirement: Family compatibility

The add-in SHALL verify that the selected tag family type can hold the values it writes, and SHALL make no change when it cannot.

#### Scenario: Reject an incompatible family

- **WHEN** the selected family type lacks a parameter the add-in writes
- **THEN** the run is rolled back whole, no tag and no text-height change remains, and the missing parameter is named in the dialog

### Requirement: Run atomicity

The add-in SHALL apply the text height and the tag changes of one run as a single reversible operation.

#### Scenario: Undo a completed run

- **WHEN** a user undoes a successful run
- **THEN** both the tag changes and any text-height change are reverted together

### Requirement: Command availability

The add-in SHALL offer tag creation only where it can be carried out, and only from a complete selection.

#### Scenario: Open on a document that cannot be tagged

- **WHEN** the command is invoked with no open project, on a family document, or on a read-only document
- **THEN** the add-in explains why in a dialog and makes no change

#### Scenario: Gate the command on a complete selection

- **WHEN** no tag family type is chosen, no spatial elements are collected, or the link source is chosen without a link
- **THEN** the create-or-update command is unavailable

### Requirement: Selection consistency

The add-in SHALL discard collected spatial elements whenever a change makes them no longer describe what would be tagged.

#### Scenario: Switch between rooms and spaces

- **WHEN** a user changes the target type
- **THEN** the phase selection and the collected elements are cleared, and the status card stops describing the previous target

#### Scenario: Turn linked sourcing on or off

- **WHEN** a user toggles the linked-model option or changes the selected link
- **THEN** the phases offered are those of the newly chosen source and the collected elements are cleared

### Requirement: Status reporting

The add-in SHALL describe what the current selection would tag using the same rules the run applies.

#### Scenario: Count what would be tagged

- **WHEN** the collected elements include elements that cannot carry a tag
- **THEN** the reported count excludes them and they are reported separately by reason


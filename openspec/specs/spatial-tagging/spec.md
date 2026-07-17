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
- **THEN** each created tag is positioned using the link instance transform applied to the linked spatial element location

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

- **WHEN** update mode is enabled and an editable tag has a SpatialElementId matching the selected spatial element unique identifier
- **THEN** the add-in updates the tag type, position, name, and number without creating another tag

### Requirement: Configurable text height

The add-in SHALL allow a valid text height to be applied to the selected tag family type and retained for a later session.

#### Scenario: Apply a valid height

- **WHEN** a user enters a valid feet-and-inches value and runs tag creation
- **THEN** the selected family type's Text Height is updated and the entered value is saved in user settings


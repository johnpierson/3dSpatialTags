# 2.0.0

**Revit 2020 – 2024 are no longer supported.** If you are on one of those, stay on 1.2.0; it
keeps working and nothing about it has changed.

The reason is the tag family. Revit families load forward but never backward, so the one family
embedded in the add-in could only work on every version at once by being saved in the oldest of
them — which meant it could never be upgraded without leaving older versions behind anyway. The
family now carries the source of each tag in parameters you can schedule and filter on, and each
supported Revit version gets its own copy built for it.

- The tag family records `SourceDocumentId` and `SourceLinkInstanceId`, so you can schedule
  which model and which link placement every tag came from. A family without those parameters
  still works — it just cannot be scheduled that way.
- Tags now remember which link instance they came from. A linked file placed more than once
  used to have its placements fight over one set of tags: tagging the second found the first's
  tags and dragged them across, leaving the first untagged on every run, with nothing said
  about it. Each placement now keeps its own. Tags placed by earlier versions are adopted and
  brought forward rather than duplicated, and the run says how many.
- Tags whose room has been deleted are reported. They used to stay in the model saying what
  they always said, as geometry that gets exported into coordination models. They are counted,
  not deleted — that is still your call.
- A text height like `1.5'` now means the same thing on every system locale. On much of
  continental Europe it was read as 15 feet and written to the family type without a word.
- The text height and the tags are now one undoable operation. A run that reported "Nothing was
  placed" had already resized the family type and left it that way, and a successful run took
  two presses of Ctrl+Z to take back.
- Duplicate-instance warnings are only hidden for tags this tool stacked on its own. It used to
  delete every one raised while it was working, including warnings about geometry it had
  nothing to do with.
- Opening the dialog on a family document, a read-only document, or with nothing open now
  explains itself instead of throwing. In a family document it could previously load the tag
  family into the family you were editing.
- The dialog is readable and usable from the keyboard: warning and error text now meet WCAG AA
  contrast, every control has a name for screen readers, tab order runs through the fields
  before the buttons, and the credit mark is reachable without a mouse.
- The status card counts what will actually be tagged. Its warning also no longer claims
  unbounded and redundant rooms cannot be tagged, because they are.
- Link transforms use the total transform, so a link nested inside another link is tagged in
  the right place.
- Failures are written to a log under `%LOCALAPPDATA%\design tech unraveled\3d Spatial Tags`.
  There was no logging at all, and every error was discarded after being shown once.
- The published `.bundle` works. Its manifest named an add-in file that was not in the zip, so
  installing that way silently did nothing. The MSIs were unaffected.
- Dependencies and the .NET SDK are pinned, with lock files per Revit version, so a release can
  be rebuilt exactly.
- Third-party licence notices now ship with the add-in.
- Installers name Design Tech Unraveled as publisher rather than whatever account built them.
- Removed about 11 MB of things nothing used: five stale DLLs, a second copy of the tag family
  embedded in every assembly, five unused images, tracked Revit backups, and leftovers of the
  old licensing module.

# 1.2.0

- Added support for Revit 2027, which runs on .NET 10.
- Fixed the Phase and link drop-downs showing `Autodesk.Revit.DB.Phase` instead of the phase or
  link name.

- Redrew the dialog in Interlude's light theme: cream ground, heavy outlines, hard shadows, one
  accent, and Space Grotesk embedded so it looks the same everywhere. The custom dark title bar is
  gone; the window uses Windows' own.
- The Create/Update button is now gated on a complete selection. It was bound to a property that
  did not exist, so it silently stayed enabled and running with nothing selected threw.
- Fixed a stale "unbounded rooms" warning that stayed on screen after switching to a phase with
  none.
- Switching between Rooms and Spaces now clears the phase and the collected elements instead of
  leaving the previous target's results armed.
- Ticking "use a linked model" now lists the link's phases rather than the host document's.
- Unloaded links are no longer offered, and a link that reports no document no longer throws.
- A tag family type that cannot hold the values this tool writes is reported, and the run rolled
  back, instead of placing tags with blank names.
- Existing tags owned by another user are reported and left alone rather than being covered with a
  duplicate.
- Restoring the saved tag-family and link selections no longer throws in a document with fewer of
  them.
- An unreadable text height is flagged in the Settings tab instead of being silently ignored.
- Revit API failures during a run are reported in the dialog rather than escaping to Revit.
- Removed unused Newtonsoft.Json, Microsoft.Net.Http and PresentationFramework.Aero2 references,
  clearing the build's compatibility warnings.

# 1.1.0

- Added support for Revit 2026.
- Updated license to GPL v3.
- Fixed URL opening method for .NET 8 compatibility.
- Updated documentation and help links to point to the new GitHub license location.

# 1.0.0

Initial release. Enjoy!
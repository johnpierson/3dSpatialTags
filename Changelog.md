# 1.2.0

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
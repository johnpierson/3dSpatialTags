# Contributing

## Spec-driven workflow

Behavior changes start as an OpenSpec change before implementation. This keeps the proposal, requirements, design decisions, implementation tasks, and lasting product specification connected.

1. Install Node.js 20 or later if `npx` is not already available.
2. Explore an idea with `/opsx:explore` when the problem or scope is still unclear.
3. Create a complete change with `/opsx:propose <description>`.
4. Review and commit the generated files under `openspec/changes/<change-name>/` before changing production code.
5. Implement the approved tasks with `/opsx:apply <change-name>` and check off each task only after its verification passes.
6. Validate the change with `npx --yes @fission-ai/openspec@1.6.0 validate <change-name> --strict --no-interactive`.
7. Archive the completed change with `/opsx:archive <change-name>`. Archiving merges its delta requirements into `openspec/specs/`, which is the source of truth for current behavior.

Use one OpenSpec change for one cohesive behavior change. Pure refactors, documentation corrections, dependency updates with no behavioral effect, and emergency build repairs may skip a proposal, but the pull request must explain why no spec is needed. If implementation reveals a requirement or design change, update the OpenSpec artifacts before continuing.

## Verification

Build the configurations affected by the change. At minimum, changes shared by all supported versions should be checked against one .NET Framework target and one .NET 8 target when the required Revit SDK assemblies are available.

Revit API and WPF behavior also requires manual validation. Record the Revit version, fixture file, host or linked-document setup, and observed result in the change tasks and pull request.


# Feature: documentation

## Originating branch
`develop`

## Goal
Bring the project documentation to a usable state for both contributors and NuGet consumers.

## Scope
1. Fix the `.csproj` description (currently wrong copy-paste text)
2. Expand the root README.md with usage examples (registration, message handlers, client/server setup)
3. Update the package README.md for NuGet consumers
4. Add XML doc comments to all public interfaces and base classes
5. Add a LICENSE file (MIT, per the badge already in the README)

## Out of scope
- Writing functional tests (separate feature)
- Fixing the `NotImplementedException` in SendMessage (separate feature)
- CHANGELOG / CONTRIBUTING files

## Acceptance criteria
- [ ] `.csproj` Description accurately describes the library
- [ ] Root README.md has installation, setup, and usage examples
- [ ] Package README.md is useful for NuGet consumers
- [ ] All public interfaces and abstract base classes have XML doc comments
- [ ] MIT LICENSE file exists
- [ ] Project builds without errors

## Done condition
All acceptance criteria are met and user confirms the feature is complete.

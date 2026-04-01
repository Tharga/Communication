# Feature: null-safe-config

## Originating branch
`develop`

## Request
From Tharga.MongoDB — see `.claude/requests.md`

## Goal
Make `AddThargaCommunicationClient` resilient to a missing `Tharga:Communication` config section so that the options callback alone can provide all required values.

## Scope
- Null-coalesce the `Get<CommunicationOptions>()` result to a new instance
- Null-coalesce `value.ServerAddress` and `value.Pattern` when building the options object
- Add tests verifying registration succeeds with no config section, with partial config, and with full config

## Acceptance criteria
- [ ] `AddThargaCommunicationClient` does not throw when `Tharga:Communication` section is missing
- [ ] Options callback can provide `ServerAddress` without config section
- [ ] Defaults are applied correctly (`Pattern` = `"hub"`, `ReconnectDelays` = `[0s, 2s, 10s, 30s]`)
- [ ] Tests cover all three scenarios

## Done condition
All acceptance criteria are met and user confirms the feature is complete.

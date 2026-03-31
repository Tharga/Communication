# Plan: null-safe-config

## Steps

- [x] **Step 1:** Write tests for the three config scenarios (no section, partial, full)
  - Added 5 tests in `CommunicationClientRegistrationTests`: no-config DoesNotThrow, AppliesOptionsCallback, AppliesDefaults, partial config MergesWithDefaults, full config UsesConfigValues
  - Added `Microsoft.Extensions.Hosting` package to test project
- [x] **Step 2:** Fix null-coalesce in `CommunicationClientRegistration.AddThargaCommunicationClient`
  - Added `?? new CommunicationOptions()` to the `Get<CommunicationOptions>()` call
- [x] **Step 3:** Verify build and tests pass — 6/6 tests pass
- [~] **Step 4:** Commit and notify Tharga.MongoDB via requests.md

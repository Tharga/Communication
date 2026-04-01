## Pending

### Null-safe config binding in AddThargaCommunicationClient
- **From:** Tharga.MongoDB (`c:\dev\tharga\Toolkit\MongoDB`)
- **Date:** 2026-03-31
- **Priority:** High
- **Description:** `CommunicationClientRegistration.AddThargaCommunicationClient` calls `configuration.GetSection("Tharga:Communication").Get<CommunicationOptions>()` which returns `null` when the config section is missing. Line 30 then accesses `value.ServerAddress`, causing a NullReferenceException. The options callback should be able to provide the server address without requiring the config section to exist. Suggested fix: `var value = configuration.GetSection("Tharga:Communication").Get<CommunicationOptions>() ?? new CommunicationOptions();`
- **Status:** Done (2026-03-31) — Null-coalesce applied, 5 tests added. Notification sent to Tharga.MongoDB.

### API key authentication for SignalR connections
- **From:** Tharga.MongoDB (`c:\dev\tharga\Toolkit\MongoDB`)
- **Date:** 2026-03-31
- **Priority:** High
- **Description:** Tharga.MongoDB needs to secure the SignalR hub used for distributed monitoring. The authentication should be built into Tharga.Communication itself so consumers don't need custom middleware. Suggested approach: (1) Client-side: allow configuring an API key via `CommunicationOptions` (code or `appsettings.json` / User Secrets), sent as a custom header during SignalR negotiation. (2) Server-side: accept a primary and secondary API key via configuration. Validate the header on connection — reject with 401 if invalid. Accept either key for zero-downtime key rotation. (3) When no keys are configured on the server, accept all connections (backwards compatible). Configuration should work via `appsettings.json`, Manage User Secrets, environment variables, or code options — no consumer-side middleware needed.
- **Status:** Pending

### Subscription-based messaging support
- **From:** Tharga.MongoDB (`c:\dev\tharga\Toolkit\MongoDB`)
- **Date:** 2026-03-31
- **Priority:** Medium
- **Description:** Tharga.MongoDB.Monitor.Client forwards monitoring data (CallDto) from remote agents to a central server via Tharga.Communication's PostAsync. Currently the client sends data on every database operation regardless of whether anyone is consuming it on the server side. A subscription mechanism is needed so the server can signal "someone is watching" and "no one is watching", allowing clients to start/stop forwarding. This avoids unnecessary network traffic and serialization when no dashboard is active. The feature should provide: (1) a way for the server to notify connected clients that a subscriber is present, (2) a way for clients to check subscription state before sending, (3) automatic updates when subscribers join or leave.
- **Status:** Pending

## Notifications

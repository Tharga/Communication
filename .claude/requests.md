## Pending

### Subscription-based messaging support
- **From:** Tharga.MongoDB (`c:\dev\tharga\Toolkit\MongoDB`)
- **Date:** 2026-03-31
- **Priority:** Medium
- **Description:** Tharga.MongoDB.Monitor.Client forwards monitoring data (CallDto) from remote agents to a central server via Tharga.Communication's PostAsync. Currently the client sends data on every database operation regardless of whether anyone is consuming it on the server side. A subscription mechanism is needed so the server can signal "someone is watching" and "no one is watching", allowing clients to start/stop forwarding. This avoids unnecessary network traffic and serialization when no dashboard is active. The feature should provide: (1) a way for the server to notify connected clients that a subscriber is present, (2) a way for clients to check subscription state before sending, (3) automatic updates when subscribers join or leave.
- **Status:** Pending

## Notifications

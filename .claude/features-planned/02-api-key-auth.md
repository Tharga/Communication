# Feature: api-key-auth

## Request
From Tharga.MongoDB — see `.claude/requests.md`

## Goal
Add API key authentication to SignalR connections so servers can restrict access without consumers needing custom middleware.

## Scope

### Client side
- Add `ApiKey` property to client `CommunicationOptions`
- Send API key as a custom header (e.g. `X-Api-Key`) during SignalR negotiation in `SignalRHostedService.BuildConnection`
- Add the header constant to `Constants.Header`

### Server side
- Add `PrimaryApiKey` and `SecondaryApiKey` properties to server `CommunicationOptions`
- Validate the `X-Api-Key` header in `SignalRHub.OnConnectedAsync` — reject with `HubException` if invalid
- When neither key is configured on the server, accept all connections (backwards compatible)
- Accept either key to support zero-downtime key rotation

### Configuration
- Configurable via `appsettings.json`, User Secrets, environment variables, or code options callback

## Steps
- [ ] Add `ApiKey` header constant to `Constants.Header`
- [ ] Add `ApiKey` property to client `CommunicationOptions`
- [ ] Send API key header in `SignalRHostedService.BuildConnection`
- [ ] Add `PrimaryApiKey` and `SecondaryApiKey` to server `CommunicationOptions`
- [ ] Validate API key in `SignalRHub.OnConnectedAsync`
- [ ] Write tests: no keys configured (accept all), valid primary key, valid secondary key, invalid key (reject), missing key when server requires one
- [ ] Update README with authentication configuration examples
- [ ] Commit and notify Tharga.MongoDB via requests.md

## Acceptance criteria
- [ ] Client sends API key header when configured
- [ ] Server rejects connections with invalid/missing key when keys are configured
- [ ] Server accepts all connections when no keys are configured (backwards compatible)
- [ ] Either primary or secondary key is accepted
- [ ] Tests cover all scenarios
- [ ] README documents the feature

## Notes
- This is a breaking behavior change for servers that configure keys — suggest minor version bump

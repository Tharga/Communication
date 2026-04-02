# Plan: fix-null-server-address

## Steps
- [ ] Guard `_serverAddress` construction against null `ServerAddress`
- [ ] In `ExecuteAsync`, return early with a log message if no server address configured
- [ ] Ensure `SendAsync` handles the no-connection case gracefully
- [ ] Write test: null ServerAddress does not throw in constructor
- [ ] Write test: ExecuteAsync exits cleanly with null ServerAddress

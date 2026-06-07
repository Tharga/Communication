# Plan: icon-and-docs

## Steps

### 1. Verify pre-requisites
- [ ] Confirm `https://thargelion.net/assets/component-communication.png` exists (HTTP 200)
- [ ] Check that GitHub Pages is enabled in repo settings (Settings → Pages — source: GitHub Actions)

### 2. PackageIconUrl migration
- [ ] Update `Tharga.Communication/Tharga.Communication.csproj` `<PackageIconUrl>` to the new URL
- [ ] Update `Tharga.Communication.Mcp/Tharga.Communication.Mcp.csproj` `<PackageIconUrl>` to the new URL
- [ ] Verify pack still works locally: `dotnet pack -c Release -p:PackageVersion=0.0.0-test`

### 3. Create docs/ scaffold
- [ ] `docs/CNAME` → `communication.tharga.net`
- [ ] `docs/docfx.json` — copy from `Tharga.Mcp/docs/docfx.json`, swap `Tharga.Mcp` → `Tharga.Communication` and icon URL
- [ ] `docs/toc.yml` (Home / Articles / API)
- [ ] `docs/index.md` — landing page (project overview, package list, quick start, link to repo)
- [ ] `docs/articles/toc.yml`
- [ ] `docs/articles/index.md` — article index
- [ ] `docs/articles/getting-started.md` — `AddThargaCommunicationServer` / `AddThargaCommunicationClient`, minimal example
- [ ] `docs/articles/messaging.md` — `PostAsync`, `SendMessage`, message handlers
- [ ] `docs/articles/subscriptions.md` — `SubscribeAsync`, `HasSubscribers`, `PostIfSubscribedAsync`, matching rules
- [ ] `docs/articles/authentication.md` — `IApiKeyValidator`, `ApiKeys`, custom validator, `IHttpContextAccessor` hint, client identity overrides
- [ ] `docs/templates/thg/public/main.css` — copy from Mcp (navbar logo size constraint)

### 4. Local docfx build
- [ ] Install docfx if not present: `dotnet tool install -g docfx`
- [ ] Run `docfx docs/docfx.json` — fix any errors, verify `_site/` looks right

### 5. GHA workflow additions
- [ ] Add `docs` job to `.github/workflows/build.yml` (mirror Tharga.Mcp lines 281–309)
- [ ] Add `docs-deploy` job (mirror Tharga.Mcp lines 311–325)
- [ ] Verify `permissions:` block has `pages: write` (already there from current workflow)

### 6. README
- [ ] Add `**Docs:** [communication.tharga.net](https://communication.tharga.net) — guides, API reference, and walkthroughs.` near the top of root README

### 7. Commit, push, PR
- [ ] Stage all changes
- [ ] Single commit: `docs: add documentation site and migrate package icon URL`
- [ ] Push branch, create PR `feature/icon-and-docs → master`

### 8. Post-merge verification (after PR merged)
- [ ] Confirm GHA pipeline runs `docs` + `docs-deploy` successfully
- [ ] Verify `https://communication.tharga.net` resolves and the site loads
- [ ] Verify next patch release packs the new icon URL
- [ ] Mark both Requests.md entries Done with the released version

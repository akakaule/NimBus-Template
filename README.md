# NimBus template — Azure DevOps

A starting point for running [NimBus](https://github.com/akakaule/NimBus) in your own Azure
subscription, deployed from Azure DevOps, with your integration contracts owned by your own
team and published to your own private Azure Artifacts feed.

Two directories, meant to become **two repositories**:

| Directory | Becomes | Owned by | Produces |
|---|---|---|---|
| [`platform/`](platform) | `Acme.Platform` | whoever owns the integration contracts | a versioned NuGet package: the event catalog |
| [`deploy/`](deploy) | `Acme.Deploy` | whoever owns the Azure environments | the deployed platform |

They are separate because they change for different reasons and at different rates. Adding an
event type is a contracts change reviewed by integration developers; changing an App Service
SKU is an infrastructure change reviewed by whoever pays for it. The only thing that crosses
between them is **a package id and a version** — the deployment never clones, reads or builds
the platform repository.

```
platform repo ──push──> Azure Artifacts feed ──pinned version──> deploy repo ──> Azure
```

## Why a catalog package at all

NimBus generates the Service Bus topology — one topic per endpoint, one subscription per
consumed event type, routing rules per event type — from a compiled `IPlatform` catalog. The
management WebApp renders Endpoints, Event Types, example payloads and PII masking from the
same object. Ship only NimBus's artifacts and you provision *its* demo catalog rather than
your integration landscape.

So `nb setup --platform-package <Id>@<Version> --platform-feed <url>` resolves your catalog
from a NuGet feed, provisions your topology from it, and stages its assembly into the WebApp
before deploying. Your contracts travel as a package, like any other dependency.

**The version is pinned, never floated.** The catalog decides the routing, so a merge in the
platform repo must not reroute a running environment on its own. Moving the pin is a reviewed
change in the deployment repo.

## Setup

Roughly 30 minutes, most of it in the Azure DevOps UI.

**1 — Split the template into two repos.** Create `Acme.Platform` and `Acme.Deploy` in your
ADO project and push `platform/` and `deploy/` into them respectively. (Keeping both in one
repo works too — you then have one pipeline per directory and two `trigger.paths` filters.)

**2 — Rename.** In the platform repo, `Acme` is a placeholder for your organisation:

| Placeholder | Where | Becomes |
|---|---|---|
| `Acme.Platform` | project, assembly, namespace, `PackageId` | your package id, e.g. `Contoso.Platform` |
| `AcmePlatform` | the `IPlatform` class | your catalog class |
| `Crm`/`Erp`/`DataPlatform`/`PartnerPortal` | endpoints + events | your actual systems |

The assembly name must not collide with an assembly the NimBus WebApp already ships — the CLI
refuses to stage one that does, rather than replacing a tested binary. Anything outside the
`NimBus.*` family is safe.

**3 — Create the feed.** Artifacts → Create Feed. Then open **Connect to feed → dotnet** and
copy the index URL into the platform pipeline's `feedIndexUrl`; the same string without
`/v3/index.json` goes into `feedBaseUrl` and into the deploy pipeline's `platformFeed`.

**4 — Grant the build identity access to the feed.** Feed settings → Permissions. This is the
step that most often bites — see [below](#when-it-goes-wrong).

**5 — Create the Azure service connection** and register the resource providers listed in
[`deploy/README.md`](deploy/README.md).

**6 — Replace every `REPLACE` parameter** in both pipelines, then register them (Pipelines →
New pipeline → Azure Repos Git → Existing YAML file).

**7 — Tag the platform repo** (`git tag 1.1.0 && git push origin 1.1.0`) so the platform
pipeline publishes a stable version — a plain push to `main` publishes only an `-rc`
pre-release — then run the deploy pipeline. The platform pipeline prints the exact
`platformPackage` line to paste into the deployment.

## When it goes wrong

Three failures with misleading messages. All three were hit while building this template.

**`the feed with ID '<feed>' doesn't exist`** — the feed exists; the URL addresses the wrong
scope. A *project-scoped* feed's URL carries a project segment
(`.../<org>/<projectId>/_packaging/<feed>/nuget`); an *organisation-scoped* one does not
(`.../<org>/_packaging/<feed>/nuget`). Copy the URL from the feed's Connect dialog rather than
composing it. Prefer the project's **id** over its name in that segment — no percent-encoding
if the name has spaces, and it survives a rename.

**`lacks permission to complete this action. You need to have 'ReadPackages'`** — the identity
your pipeline actually runs as has no role on the feed. Two traps here:

- The identity is often **`Project Collection Build Service (<org>)`**, not
  `<Project> Build Service`, because job authorization scope is collection-wide by default.
  The error names the identity's GUID; match it against the row you added.
- **Collaborator is not enough to push.** "Feed and Upstream Reader (Collaborator)" reads and
  pulls from upstreams; publishing needs **Feed Publisher (Contributor)**. The deploy pipeline
  only reads, so Reader suffices for it; the platform pipeline needs Contributor.

**A green deployment that shows the wrong endpoints** — the WebApp falls back to NimBus's
built-in catalog when `NimBus__PlatformType` / `NimBus__PlatformAssembly` are missing, which
looks exactly like success. The deploy pipeline's verify step asserts both, plus that the
catalog's endpoints have topics in the namespace.

## Versions

This template is pinned to **NimBus 3.4.0**, in three places that must move together:

- `deploy/pipelines/azure-pipelines-deploy.yml` → `nimbusVersion`
- `platform/Directory.Build.props` → `NimBusVersion` (the `Akaule.NimBus.Abstractions` your
  catalog compiles against — the CLI reflection-loads it, so they must be compatible)
- `platform/pipelines/azure-pipelines-platform.yml` → `nimbusVersion`

## What this is not

Not a runtime. Your adapters — the services that publish and handle events — are your own
applications; this template deploys the platform they talk to and the catalog that describes
them. See NimBus's [getting-started](https://github.com/akakaule/NimBus/blob/master/docs/getting-started.md)
and [sdk-api-reference](https://github.com/akakaule/NimBus/blob/master/docs/sdk-api-reference.md)
for writing those.

Not the only way to deploy NimBus either — `nb setup` runs perfectly well from a laptop or a
GitHub Actions workflow. See
[deployment.md](https://github.com/akakaule/NimBus/blob/master/docs/deployment.md).

## License

MIT, same as NimBus. Copy it, fork it, strip it down.

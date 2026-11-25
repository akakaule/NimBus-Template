# Acme.Platform

> **Template.** This directory is meant to become **its own Azure DevOps repository**, owned
> by whoever owns your integration contracts. See the [root README](../README.md) for how to
> split it out and what to rename. The `Acme` name and the CRM/ERP example below are
> placeholders — replace them with your own systems.

The **platform catalog** for the Acme integration landscape: which systems are
connected, which events flow between them, and which fields are personal data.

This repository produces exactly one thing — the NuGet package
**`Acme.Platform`**, published to a private Azure Artifacts feed. It contains
no runtime, no adapters and no deployment logic. NimBus reads the catalog out of the package:

| What reads it | What it does with it |
|---|---|
| `nb topology apply` / `nb setup` | Provisions the Service Bus topology — one topic per endpoint, one subscription per consumed event type, routing rules per event type |
| NimBus management WebApp | Renders Endpoints, Event Types, example payloads and PII masking |

Because the catalog decides the topology, **the deployment pins an explicit version**. A
floating version would let a merge here reroute production traffic without a review.

## The catalog

| Endpoint | System | Produces | Consumes |
|---|---|---|---|
| `CrmEndpoint` | Crm | `CrmAccountCreated`, `CrmAccountUpdated`, `CrmContactCreated`, `ErpCreditCheckRequested`, `PlaceCustomerOnCreditHold` | `ErpCustomerCreated`, `ErpCustomerUpdated`, `PartnerLeadSubmitted` |
| `ErpEndpoint` | Erp | `ErpCustomerCreated`, `ErpCustomerUpdated` | `CrmAccountCreated`, `CrmAccountUpdated`, `ErpCreditCheckRequested`, `PlaceCustomerOnCreditHold` |
| `DataPlatformEndpoint` | DataPlatform | — | all seven business events |
| `PartnerPortalEndpoint` | PartnerPortal | `PartnerLeadSubmitted` | — |

Three details worth knowing before you add anything:

- **Event fields can be structured.** `CrmAccountCreated.RegisteredAddress` is an `Address`
  — a plain class, not an `Event` — that arrives as a nested JSON object. Its members carry
  their own descriptions and validation, and the management UI, the example payload and the
  catalog export all render it as a type of its own. `[Sensitive]` on such a class masks
  every member at once.
- **`PlaceCustomerOnCreditHold` is a `Command`**, not an event. A command instructs one system
  to act, so the catalog must declare **exactly one** consumer. Zero dead-letters every send;
  two silently turn an instruction into a broadcast. `PlatformValidation` enforces it at
  provisioning time and `PlatformCatalogTests` enforces it here, before publishing.
- **`CrmContactCreated` carries the PII demonstration.** Email is hashed (equal inputs stay
  equal, so repeat failures are still correlatable), phone reveals its last four digits, the
  national identification number is fully redacted, and the operational fields — account,
  role, opt-in — stay readable so a failed message can be triaged without PII access.

Endpoint class names become **topic names** and event class names become **routing keys**, so
renaming either is a topology change, not a refactor. The tests pin both sets.

## Working on it

```bash
dotnet build Acme.Platform.slnx -c Release
dotnet test  Acme.Platform.slnx -c Release
```

To see what NimBus will make of a change before publishing anything, point the CLI at the
build output — this is the same reflection load the deployment performs:

```bash
dotnet tool install --global Akaule.NimBus.CommandLine --version 5.6.0

# The topology NimBus would provision
nb topology export --assembly src/Acme.Platform/bin/Release/net10.0/Acme.Platform.dll \
  --output platform-config.json

# Browsable EventCatalog docs (MDX + per-service AsyncAPI)
nb catalog export --assembly src/Acme.Platform/bin/Release/net10.0/Acme.Platform.dll \
  --output eventcatalog
```

The pipeline runs both on every build and publishes them as artifacts, so a failed catalog
load fails the build instead of the deployment.

## Publishing

`pipelines/azure-pipelines-platform.yml` runs on every push to `main` or `feature/*` and on
every tag: build → test → export the catalog → `dotnet pack` → push to the Azure Artifacts
feed with `--skip-duplicate`. Pull requests build and validate but never publish.

Nothing in the repo carries a version number. [GitVersion](gitversion.yml) computes it from
git, so what you push decides what is published:

| You push | Published version | Use it for |
|---|---|---|
| `feature/notifications` | `1.0.1-notifications.3` | trying a catalog against a dev environment |
| `main` (merge) | `1.0.1-rc.2` | release candidate — test/UAT-style pins |
| tag `1.0.1` on `main` | `1.0.1` | the stable version a deployment pins |

The patch number counts up from the last tag, so a release is just a tag:

```bash
git tag 1.0.1 && git push origin 1.0.1
```

The first stable version is `next-version` in `gitversion.yml` (`1.1.0`) until a tag exists;
after that the tag wins and the line no longer matters. Before the deployment can pin a
version, one must exist on the feed: a bare `main` build publishes only an `-rc` pre-release,
so tag it.

## Consuming it

The deployment pipeline in [`../deploy`](../deploy) resolves this package by pinned version:

```bash
nb setup --solution-id acme --environment dev --resource-group rg-acme-dev \
  --platform-package Acme.Platform@1.0.0 \
  --platform-feed https://pkgs.dev.azure.com/<org>/<projectId>/_packaging/<feed>/nuget
```

Three things that are easy to get wrong — all three are covered in the
[root README's troubleshooting section](../README.md#when-it-goes-wrong):

- **`--platform-feed` takes the feed base, not the service index.** The CLI appends
  `/v3/index.json` itself and then reads the flat-container address from the index. Passing
  the full `.../nuget/v3/index.json` produces a 404 that reads like a missing package.
- **A project-scoped feed's URL carries a project segment**; an organisation-scoped feed's
  does not. Addressing the wrong scope fails with *"the feed with ID '<feed>' doesn't
  exist"*, which reads like a missing feed.
- **A private feed needs `NIMBUS_PLATFORM_FEED_TOKEN`**, sent as the password half of Basic
  auth. In Azure DevOps that is `$(System.AccessToken)` — provided the identity your
  pipelines actually run as has been granted access to the feed, which is not automatic.

Nothing else about this repository is involved in a deployment — no clone, no submodule, no
build of this solution. The feed is the entire interface.

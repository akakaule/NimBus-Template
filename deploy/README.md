# Deployment

> **Template.** This directory is meant to become **its own Azure DevOps repository**, owned
> by whoever owns your Azure environments. See the [root README](../README.md) for the
> end-to-end setup.

One pipeline — [`pipelines/azure-pipelines-deploy.yml`](pipelines/azure-pipelines-deploy.yml)
— deploys all of NimBus into an Azure subscription and then proves what it deployed.

Nothing in this repository is read by the deployment. The applications come from nuget.org
and the catalog comes from your Azure Artifacts feed; the checkout exists only to give the
pipeline its own YAML. There is no NimBus clone, no submodule, no Node.js and no build.

## What one run does

| Step | What it does |
|---|---|
| Install `nb` | `dotnet tool install --global Akaule.NimBus.CommandLine --version <pinned>` |
| `nb setup` | Creates the resource group if missing, provisions the infrastructure from the bicep the CLI carries, provisions the Service Bus topology from **your** catalog, and zip-deploys the Resolver and the management WebApp with your catalog assembly staged into it |
| Verify | Four assertions, below |

`nb setup` is idempotent — re-running it reconciles rather than rebuilds.

## What it provisions

Service Bus namespace, Azure SQL (or Cosmos DB, per `storageProvider`), storage account,
Application Insights, an App Service plan with the management WebApp, and a Function App for
the Resolver — plus **role assignments**, which is why the service connection's identity needs
Owner, or Contributor plus Role Based Access Control Administrator. The full inventory,
provider registrations and RBAC matrix are in NimBus's
[azure-requirements.md](https://github.com/akakaule/NimBus/blob/master/docs/azure-requirements.md).
Read it before the first run: this creates billable resources.

## What the verify step asserts

A deployment that silently deploys the *wrong* thing looks identical to a successful one, so
the last step checks four things from outside the deployment:

1. **The WebApp is live** — `/alive` returns 200 (an anonymous health endpoint).
2. **The deployed binaries are the pinned version** — read from `NimBus.WebApp.deps.json` in
   the deployed site over SCM/Kudu, using the same ARM token the deployment used.
3. **Your catalog shipped** — `NimBus__PlatformType` / `NimBus__PlatformAssembly` are set on
   the WebApp and the named assembly is actually present in the site. Without these the app
   silently falls back to NimBus's built-in demo catalog.
4. **The topology matches the catalog** — every endpoint in the catalog has a topic in the
   namespace. The expectation is re-exported from the same package the deployment resolved,
   so it cannot drift from the catalog the way a hardcoded list would.

Note the version is deliberately **not** read from `GET /api/app/stats`. That endpoint stays
anonymous for liveness probes, but its payload is authenticated-only, and a pipeline's service
principal is an Azure RBAC identity rather than a WebApp user — it gets
`{"env":null,"platformVersion":null,…}` no matter what.

## Before the first run

1. Replace every parameter marked `REPLACE` in the pipeline.
2. Create the Azure service connection named in `azureServiceConnection`.
3. Register the required resource providers on the subscription: `Microsoft.ServiceBus`,
   `Microsoft.Web`, `Microsoft.Storage`, `Microsoft.Insights`, `Microsoft.DocumentDB`,
   `Microsoft.EventGrid`.
4. Grant the identity your pipelines run as **read** access to the platform feed.
5. Define the **secret** pipeline variables the chosen store needs — the run fails before it
   touches Azure if one is missing:
   - `NIMBUS_SQL_ADMIN_PASSWORD` for `storageProvider: sqlserver` + `sqlMode: provision`
   - `NIMBUS_SQL_CONNECTION_STRING` for `sqlMode: external`
   - `NIMBUS_IDENTITY_ADMIN_PASSWORD` when `identityAdminEmail` is set (seeds a
     username/password admin so a torn-down environment comes back with a sign-in; the
     alternative is configuring `AzureAd__*` on the WebApp by hand after each teardown)

To do a first run before your catalog exists, set `platformPackage` to an empty string — that
deploys NimBus's built-in demo catalog, which is a good way to separate "does my Azure setup
work" from "does my catalog work".

## Upgrading NimBus

Bump `nimbusVersion`. The CLI version selects the deployment artifacts, so an upgrade is a
reviewed change to this file and never something a release on nuget.org does to a running
environment on its own. Keep `NimBusVersion` in the platform repo's `Directory.Build.props` on
the same number — the CLI reflection-loads your catalog assembly, so the two must be
compatible.

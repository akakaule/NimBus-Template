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
3. Register the resource providers on the subscription:
   - always: `Microsoft.ServiceBus`, `Microsoft.Web`, `Microsoft.Storage`, `Microsoft.Insights`
   - the store: `Microsoft.Sql` for `storageProvider: sqlserver` + `sqlMode: provision` (the
     default), or `Microsoft.DocumentDB` for `cosmos`
   - `networkMode: private`: `Microsoft.Network`, plus `Microsoft.App` on Flex Consumption
   - `Microsoft.EventGrid` only if you use Event Grid storage hooks; `nb` tries to register it
     itself and only warns if it can't
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

## Private networking

`networkMode: private` puts Service Bus, the store, the Functions storage and both apps behind
private endpoints in your subnets and turns public access off. It is opt-in; the defaults
deploy publicly, as before. NimBus's
[private-networking.md](https://github.com/context-and-oss/NimBus/blob/master/docs/private-networking.md)
is the full guide. What the pipeline needs from you:

- **An agent inside the network.** Set `agentPool` to a self-hosted pool or a Managed DevOps
  Pool in (or peered to) the VNet. It must resolve the privatelink zones and have the Azure
  CLI, plus egress to Azure Resource Manager, Entra ID, Azure DevOps and nuget.org. The run
  refuses `private` on the Microsoft-hosted pool.
- **Three subnets** — `privateEndpointSubnetId` (not delegated), `resolverSubnetId` and
  `webAppSubnetId` (delegated as the parameter descriptions say) — and a DNS choice in
  `privateDns`, plus `monitorPrivateLink: none`. The service connection's identity needs
  Network Contributor on the subnets, and Private DNS Zone Contributor on the zones'
  resource group for `privateDns: existing`.
- **Egress from the apps to Entra ID** through your firewall, before the first private run:
  both apps route all outbound traffic into the VNet, managed-identity token requests
  included.

`nb` records the network setup on the resource group, so only the first private run needs the
subnets and DNS settings; leave the rest on `auto` afterwards.

A **new** environment needs one run. An **existing** public one moves in steps:

1. Service Bus must be Premium, and Azure can't convert a Standard namespace in place, so the
   move lands on a new namespace and does not copy messages. Quiesce publishers and let the
   Resolver and subscribers drain first.
2. Run with `networkMode: private`, the network settings, `allowPublicAccess` ticked and a
   new `serviceBusNamespaceName` (e.g. `sb-acme-prod-premium`). This adds the Premium
   namespace, private endpoints, DNS and VNet integration while public access stays on;
   `nb` and the verify step follow the recorded namespace name from then on. Re-point your
   adapters at the new namespace and check the NimBus names resolve to private addresses
   from the apps and from your adapters' networks.
3. Run again with `networkMode: private` and `allowPublicAccess` unticked to turn public
   access off. Choose `private` explicitly: on `auto`, `nb` reproduces the recorded,
   unfinished transition.

`networkMode: public` rolls back, deleting only the network resources `nb` created.

## Resolver capacity

`resolverMaxSessions` (default 16 per instance) and `resolverMaxInstances` bound how hard the
Resolver drives the message store. Raise them only together with the store's throughput:
200 sessions per instance throttled the store in production, which is why NimBus moved the
default to 16.

## Upgrading NimBus

Bump `nimbusVersion`. The CLI version selects the deployment artifacts, so an upgrade is a
reviewed change to this file and never something a release on nuget.org does to a running
environment on its own. Keep `NimBusVersion` in the platform repo's `Directory.Build.props` on
the same number — the CLI reflection-loads your catalog assembly, so the two must be
compatible.

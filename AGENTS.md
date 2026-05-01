# GRACE Framework - Project Engineering Protocol

## Keywords
mif, identity-management, keycloak, cqrs, reader, writer, worker

## Annotation
IdentityService microservice in MIF. Solution file: **IdentityServices.sln**. .NET 9 CQRS: Reader (query-only user/role cache), Writer (command gateway), Worker (saga processor + Keycloak sync). MongoDB caches Keycloak state.

## Core Principles
1. **Never Write Code Without a Contract** — MODULE_CONTRACT defines PURPOSE, SCOPE, INPUTS, OUTPUTS
2. **Semantic Markup Is Load-Bearing** — `// START_BLOCK_<NAME>` are navigation anchors
3. **Knowledge Graph Is Always Current** — `docs/knowledge-graph.xml` is the project map
4. **Verification Is First-Class** — Testing, traces, log anchors designed before execution
5. **Top-Down Synthesis** — Requirements → Technology → Development → Verification → Code
6. **Governed Autonomy** — Freedom in HOW, not WHAT

## File Structure
```
docs/
  requirements.xml       - Use cases, constraints, risks
  technology.xml         - Stack, tooling
  development-plan.xml   - Modules, phases, data flows
  verification-plan.xml  - Test strategy, gates
  knowledge-graph.xml    - Module dependencies
  operational-packets.xml- Execution templates
src/
  IdentityService.Shared/          - DTOs, events, commands
  IdentityService.Reader/          - Query API (GET endpoints)
  IdentityService.Writer/          - Command API (POST → MQ)
  IdentityService.Worker/          - Saga processor (MQ subscriber)
  IdentityService.KeycloakAdapter/ - Keycloak admin client + JWT validator
  IdentityService.CacheLayer/      - MongoDB user/role cache
tests/
  IdentityService.*.UnitTests/
  IdentityService.IntegrationTests/
  IdentityService.LoadTests/
deploy/
  k8s/                             - Kubernetes + Dapr manifests
```

## Semantic Markup (C# .NET)

Module contract:
```csharp
// START_MODULE_CONTRACT
//   PURPOSE: [Single sentence]
//   SCOPE: [Operations]
//   DEPENDS: [Module dependencies]
// END_MODULE_CONTRACT
```

Function contract:
```csharp
// START_CONTRACT: functionName
//   PURPOSE: [What it does]
//   INPUTS: { paramName: Type - description }
//   OUTPUTS: { ReturnType - description }
// END_CONTRACT: functionName
```

Code blocks:
```csharp
// START_BLOCK_QUERY_CACHE
// ... code ...
// END_BLOCK_QUERY_CACHE
```

## Logging Convention
```csharp
_logger.Information("[Reader][GetUser][BLOCK_QUERY_CACHE] Querying user {userId}", userId);
```

Rules: Prefix: `[ModuleName][functionName][BLOCK_NAME]`. Structured fields only. Never log email, phone, tokens, PII. Redact at enricher level.

## Verification Conventions
- Deterministic assertions first (xUnit + FakeItEasy)
- Log/trace assertions for saga state transitions
- Bottom-up integration: MongoDB → Reader (cache queries) + Writer (command routing) → Worker (saga)
- Module-local tests close to modules
- Wave and phase checks explicit in verification-plan.xml

## Rules for Modifications
1. Read MODULE_CONTRACT before editing
2. After editing, update MODULE_MAP
3. After adding/removing modules, update docs/knowledge-graph.xml
4. After changing tests/commands/logs, update docs/verification-plan.xml
5. After fixes, add CHANGE_SUMMARY and strengthen nearby verification
6. Never remove semantic markup anchors

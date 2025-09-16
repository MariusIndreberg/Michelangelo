Love the tree idea. Here’s a tight design that lets an LLM “orchestrate” analysis while keeping the heavy lifting deterministic and auditable.

1) Overall flow (tree planner)

Detect languages (breadth-first)

LLM gets a file manifest (names, sizes, top N paths).

It returns a LanguagePlan (e.g., ["ts", "python", "go"]) with confidence + where to scan.

Per-language branch

For each language, the LLM calls language-specific tools that produce a CallGraphFragment (nodes/edges + evidence).

Merge & normalize

Deterministic merger → one UnifiedGraph (IDs, kinds, edge types, confidence).

Render

Convert graph → Mermaid + Excalidraw (using dagre for layout).

(Optional) LLM polish

LLM can rename/group nodes and craft short edge labels, but cannot add/remove nodes/edges.

2) Tool anatomy (function-calling style)

Have a fixed registry the LLM can call. Tools are deterministic, sandboxed, and return JSON + evidence.

Core tools

repo.list_tree({max_files, include_globs, exclude_globs}) → file list (path, bytes, sha)

repo.read_text({path, max_bytes}) → text

repo.grep({pattern, globs}) → matches with file:line

graph.merge({fragments[]}) → UnifiedGraph

graph.render_mermaid({graph}) → .mmd

graph.render_excalidraw({graph}) → .excalidraw

Language tools (examples)

lang.ts.callgraph({entry_globs, tsconfig_path?})

Uses TS compiler API/Babel to find imports + call sites; detects axios/fetch/gRPC/kafka.

lang.python.callgraph({roots, venv_mode:"off"|"inspect"})

Uses ast, import resolution, simple const-prop for URLs; detects requests, grpc, kafka.

lang.go.callgraph({module_root})

Runs go list, gopls/static callgraph; detects net/http, gRPC, sql DSNs.

lang.java.callgraph({module_root, build:"maven"|"gradle"})

Bytecode or LSP references; Spring controllers/routes, Feign/RestTemplate.

lang.csharp.callgraph({solution_path})

Roslyn analyzers; ASP.NET endpoints, HttpClient, EF connection strings.

infra.compose.parse({compose_paths[]}) → services, ports, env, depends_on

infra.k8s.parse({yaml_globs}) → Deployments/Services/Ingresses

apis.openapi.parse({globs}), apis.grpc.parse({proto_globs}), apis.asyncapi.parse({globs})

All language tool outputs share a fragment schema:

{
  "nodes": [
    { "id":"svc.orders", "kind":"service", "label":"orders", "file":"services/orders/main.go" }
  ],
  "edges": [
    {
      "from":"svc.api", "to":"svc.orders", "kind":"http",
      "detail":"POST /v1/orders", "confidence":0.72,
      "evidence":[{"type":"code","file":"src/api/routes.ts","line":42,"snippet":"axios.post(`${ORDERS}/v1/orders`, body)"}]
    }
  ],
  "hints": { "serviceCandidates":["orders","payments"] }
}

3) Orchestration prompts (compact)

A. Language detection (planner)
Input: top 2–3k file paths (no contents), sizes, and first 100 bytes for a small random sample.
Task: return languages + locations.

Output schema:

{
  "languages": [
    { "id":"ts", "confidence":0.95, "roots":["services/api","packages/*"] },
    { "id":"python", "confidence":0.7, "roots":["jobs/","tools/"] }
  ],
  "infra": ["compose","k8s","openapi"]
}


B. Branch execution
For each language in languages with confidence ≥ 0.4:

Call the matching lang.<id>.callgraph(...) with the roots.

Optionally call infra.*.parse in parallel when present.

C. Merge & sanity check

Call graph.merge.

Ask LLM to validate labels/grouping only:

“Rename nodes for readability; group into domains; summarize edge labels in ≤4 words; DO NOT add/remove graph elements.”

4) Confidence model (simple, additive)

+0.5: code evidence of call (client stub or http call with resolvable constant host/path).

+0.3: corroborated by infra (compose service name, k8s service, env var → same host).

+0.2: API spec match (OpenAPI/grpc method).

−0.2: unresolved target (e.g., string concat / reflection).

Render solid if ≥0.6 else dashed.

5) Diagram generation

Mermaid (fast preview):

graph LR with swimlanes: Clients → Gateways → Services → Data.

Edge label: HTTP GET /orders, gRPC Orders/Create, pub topic-x, db write.

Excalidraw:

dagre-coordinates, shapes by kind (service=rounded rect, db=cylinder, queue=parallelogram).

Store evidence in customData for click-to-view in your UI.

6) Security & scale

Never execute repo code; static parsing only.

Run analyzers in a firejail/namespace with CPU/memory limits.

Sample very large repos (e.g., cap per-language files at N, then request more if coverage < threshold).

Cache parse trees per file hash; incremental runs diff-only.

7) Failure & fallback strategy

If a language tool fails or coverage < X%:

Fallback to greps for common client patterns to at least produce inter-service edges.

Mark edges confidence ≤ 0.4 and render dashed.

If no infra files found:

Heuristically map hosts like http://orders:8080 to service orders by directory or compose-like naming.

8) Minimal contracts you can implement this week

Tool I/O stubs (TypeScript)

type Evidence = { type: "code"|"config"; file: string; line?: number; snippet?: string };
type Node = { id:string; kind:"service"|"module"|"func"|"db"|"queue"|"external"; label:string; file?:string };
type Edge = { from:string; to:string; kind:"call"|"http"|"grpc"|"pub"|"sub"|"db"; detail?:string; confidence:number; evidence:Evidence[] };
type Fragment = { nodes:Node[]; edges:Edge[]; hints?:Record<string,unknown> };


Planner → executor skeleton

const manifest = await repo.list_tree({...});
const plan = await llm.planLanguages(manifestSummary);
const frags = await Promise.all([
  ...plan.languages.map(l => tools[`lang.${l.id}.callgraph`](l.roots)),
  plan.infra.includes("compose") ? tools["infra.compose.parse"]({...}) : null,
  plan.infra.includes("k8s") ? tools["infra.k8s.parse"]({...}) : null,
].filter(Boolean));
const graph = await graph.merge(frags);
const pretty = await llm.labelGraph(graph); // labels/groups only
await graph.render_mermaid(pretty);
await graph.render_excalidraw(pretty);

9) Why this works with an LLM “tree”

The LLM decides which branches to explore (languages, infra) and how deep.

Deterministic tools discover structure; LLM humanizes it.

The UnifiedGraph + evidence gives you diffable, auditable outputs per commit.

If you want, I can draft the JSON schemas for each lang.*.callgraph tool (inputs/outputs) and a first-pass lang.ts.callgraph strategy that finds HTTP/gRPC edges and module call graphs.
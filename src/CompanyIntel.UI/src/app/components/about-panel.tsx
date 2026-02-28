"use client";

const techDecisions = [
  { component: "LLM", choice: "Llama 3.1 via Ollama", why: "Local inference, no API keys needed" },
  { component: "Embeddings", choice: "all-minilm via Ollama", why: "384-dim dense embeddings, fast on CPU" },
  { component: "Vector Store", choice: "Qdrant", why: "Persistent vector DB, Aspire integration" },
  { component: "Ingestion", choice: "PdfPig", why: "Pure .NET PDF text extraction" },
  { component: "Agent Framework", choice: "MAF + AG-UI", why: "Microsoft Agent Framework with AG-UI protocol" },
  { component: "AI Abstractions", choice: "Microsoft.Extensions.AI", why: "Vendor-neutral IChatClient + IEmbeddingGenerator" },
  { component: "UI", choice: "CopilotKit + Next.js 15", why: "Production-ready chat components, React 19" },
  { component: "Orchestration", choice: ".NET Aspire", why: "Service discovery, built-in OTel dashboard" },
];

const principles = [
  { name: "Offline-First", desc: "Retrieval phase needs zero internet" },
  { name: "Reproducibility", desc: "Deterministic chunk IDs, raw files preserved" },
  { name: "Citations Everywhere", desc: "Every chunk carries source info, every answer references sources" },
  { name: "Grounding", desc: 'System prompt enforces "answer ONLY from retrieved context"' },
  { name: "Open Standards", desc: "AG-UI, OpenTelemetry, Aspire (all open-source)" },
];

export function AboutPanel() {
  return (
    <div className="h-[calc(100vh-3.5rem)] w-screen overflow-y-auto bg-gray-50 px-8 py-6">
      <div className="mx-auto max-w-[95vw] space-y-6">
        <div className="rounded-xl border border-gray-200 bg-white p-6">
          <h2 className="text-lg font-semibold text-gray-800 mb-2">Two-Phase Architecture</h2>
          <div className="space-y-3 text-sm text-gray-700">
            <p>
              <span className="font-semibold text-gray-900">Phase 1 — Ingest:</span>{" "}
              Upload PDF documents via the Upload tab. The backend extracts text using PdfPig,
              chunks it semantically (256-384 tokens), embeds with all-minilm (384-dim),
              and stores everything in Qdrant.
            </p>
            <p>
              <span className="font-semibold text-gray-900">Phase 2 — Retrieval (Offline):</span>{" "}
              The chat agent embeds the user&apos;s question, runs vector search against Qdrant,
              and feeds the top chunks to Llama 3.1 for a grounded, citation-backed answer.
              Zero internet needed at query time.
            </p>
          </div>
        </div>

        <div className="rounded-xl border border-gray-200 bg-white p-6">
          <h2 className="text-lg font-semibold text-gray-800 mb-3">Key Technology Decisions</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 text-left text-gray-500">
                  <th className="pb-2 pr-4 font-medium">Component</th>
                  <th className="pb-2 pr-4 font-medium">Choice</th>
                  <th className="pb-2 font-medium">Why</th>
                </tr>
              </thead>
              <tbody>
                {techDecisions.map((d) => (
                  <tr key={d.component} className="border-b border-gray-100">
                    <td className="py-2 pr-4 font-semibold text-gray-900 whitespace-nowrap">{d.component}</td>
                    <td className="py-2 pr-4 text-gray-700 whitespace-nowrap">{d.choice}</td>
                    <td className="py-2 text-gray-600">{d.why}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="rounded-xl border border-gray-200 bg-white p-6">
          <h2 className="text-lg font-semibold text-gray-800 mb-3">Design Principles</h2>
          <ul className="space-y-2 text-sm">
            {principles.map((p) => (
              <li key={p.name} className="flex gap-2">
                <span className="font-semibold text-gray-900 whitespace-nowrap">{p.name}</span>
                <span className="text-gray-400">—</span>
                <span className="text-gray-600">{p.desc}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}

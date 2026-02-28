"use client";

import { useEffect, useCallback } from "react";

interface Tool {
  name: string;
  description: string;
  params: { name: string; type: string; optional?: boolean }[];
}

const chatTools: Tool[] = [
  {
    name: "search_knowledge_base",
    description: "Search the knowledge base for Company Intelligence",
    params: [
      { name: "query", type: "str" },
      { name: "company", type: "str", optional: true },
    ],
  },
];

function ToolCard({ tool }: { tool: Tool }) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white p-4">
      <code className="text-sm font-semibold text-gray-900">
        {tool.name}
        <span className="text-gray-400">
          ({tool.params
            .map((p) => `${p.name}${p.optional ? "?" : ""}: ${p.type}`)
            .join(", ")})
        </span>
      </code>
      <p className="mt-1.5 text-sm text-gray-500">{tool.description}</p>
    </div>
  );
}

export function HelpModal({ onClose }: { onClose: () => void }) {
  const handleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    },
    [onClose],
  );

  useEffect(() => {
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [handleKeyDown]);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-lg mx-4 rounded-xl bg-gray-50 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
          <h2 className="text-base font-semibold text-gray-800">
            Available Tools — Chat
          </h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 cursor-pointer text-xl leading-none"
          >
            ✕
          </button>
        </div>

        <div className="flex flex-col gap-3 p-6 max-h-[60vh] overflow-y-auto">
          {chatTools.map((t) => (
            <ToolCard key={t.name} tool={t} />
          ))}
        </div>
      </div>
    </div>
  );
}

"use client";

import { useState, useEffect } from "react";
import { CopilotKitProvider } from "@copilotkit/react-core/v2";
import { ChatPanel } from "./chat-panel";
import { HelpModal } from "./help-modal";
import { AboutPanel } from "./about-panel";
import { DocumentsPanel } from "./documents-panel";

type Tab = "chat" | "documents" | "about";

const tabs: { id: Tab; label: string; activeColor: string }[] = [
  { id: "chat", label: "Chat", activeColor: "text-blue-600 border-blue-600" },
  { id: "documents", label: "Documents", activeColor: "text-emerald-600 border-emerald-600" },
];

const fallbackSuggestions = [
  "What companies are in the knowledge base?",
  "Summarize the key findings from the documents",
].map((ex) => ({ title: ex, message: ex }));

export function AppShell() {
  const [activeTab, setActiveTab] = useState<Tab>("chat");
  const [helpOpen, setHelpOpen] = useState(false);
  const [suggestions, setSuggestions] = useState(fallbackSuggestions);

  useEffect(() => {
    fetch("/api/suggestions")
      .then((res) => (res.ok ? res.json() : Promise.reject()))
      .then((data) => {
        if (Array.isArray(data) && data.length > 0) setSuggestions(data);
      })
      .catch(() => {});
  }, []);

  return (
    <>
      <nav className="h-14 flex items-center gap-6 px-6 border-b border-gray-200 bg-white">
        <button
          onClick={() => setActiveTab("about")}
          className="font-semibold text-gray-800 mr-4 cursor-pointer hover:text-blue-600 transition-colors"
        >
          Company Intelligence
        </button>
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`text-sm pb-0.5 cursor-pointer ${
              activeTab === tab.id
                ? `border-b-2 font-medium ${tab.activeColor}`
                : "text-gray-500 hover:text-gray-700"
            }`}
          >
            {tab.label}
          </button>
        ))}
        <button
          onClick={() => setHelpOpen(true)}
          className="ml-auto w-7 h-7 rounded-full border border-gray-300 text-gray-400 hover:text-gray-600 hover:border-gray-400 text-sm font-medium cursor-pointer transition-colors"
        >
          ?
        </button>
      </nav>

      {helpOpen && activeTab === "chat" && (
        <HelpModal onClose={() => setHelpOpen(false)} />
      )}

      {activeTab === "about" && <AboutPanel />}

      <CopilotKitProvider runtimeUrl="/api/copilotkit">
        <div className={`h-[calc(100vh-3.5rem)] w-screen flex flex-col bg-gray-50 ${activeTab !== "chat" ? "hidden" : ""}`}>
          <div className="flex-1 min-h-0 px-8 pt-4">
            <ChatPanel
              agentId="agentic_chat"
              welcomeMessage="What company would you like to know about?"
              suggestions={suggestions}
            />
          </div>
          <footer className="border-t border-gray-200 bg-white px-8 py-2">
            <span className="text-xs text-gray-300">
              Answers from stored data only
            </span>
          </footer>
        </div>
      </CopilotKitProvider>

      {activeTab === "documents" && (
        <div className="h-[calc(100vh-3.5rem)] w-screen overflow-y-auto bg-gray-50 px-8">
          <DocumentsPanel />
        </div>
      )}
    </>
  );
}

"use client";

import { useState, useCallback } from "react";
import { CopilotChat, useConfigureSuggestions } from "@copilotkit/react-core/v2";

interface ChatPanelProps {
  agentId: string;
  welcomeMessage: string;
  suggestions?: { title: string; message: string }[];
}

export function ChatPanel({ agentId, welcomeMessage, suggestions = [] }: ChatPanelProps) {
  useConfigureSuggestions(
    {
      suggestions,
      available: "before-first-message",
      consumerAgentId: agentId,
    },
    [suggestions, agentId],
  );
  const [chatKey, setChatKey] = useState(0);

  const handleNewConversation = useCallback(() => {
    setChatKey((k) => k + 1);
  }, []);

  return (
    <div className="h-full flex flex-col">
      <div className="flex items-center justify-between px-2 pb-2">
        <span className="text-xs text-gray-400 tracking-wide uppercase">Conversation</span>
        <button
          onClick={handleNewConversation}
          className="inline-flex items-center gap-1.5 text-sm text-gray-500 bg-white border border-gray-200 rounded-lg px-3 py-1.5 hover:bg-gray-50 hover:border-gray-300 hover:text-gray-700 transition-all cursor-pointer shadow-sm"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="w-3.5 h-3.5">
            <path d="M12 20h9" /><path d="M16.5 3.5a2.121 2.121 0 1 1 3 3L7 19l-4 1 1-4Z" />
          </svg>
          New conversation
        </button>
      </div>
      <CopilotChat
        key={chatKey}
        className="flex-1 rounded-2xl"
        labels={{ welcomeMessageText: welcomeMessage }}
        agentId={agentId}
      />
    </div>
  );
}

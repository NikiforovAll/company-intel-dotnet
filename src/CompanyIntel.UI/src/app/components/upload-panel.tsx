"use client";

import { useState, useCallback, useRef } from "react";

type UploadStatus = "idle" | "uploading" | "success" | "error";

interface UploadResult {
  fileName: string;
  status: UploadStatus;
  message?: string;
}

export function UploadPanel() {
  const [dragOver, setDragOver] = useState(false);
  const [results, setResults] = useState<UploadResult[]>([]);
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const uploadFile = useCallback(async (file: File) => {
    const entry: UploadResult = { fileName: file.name, status: "uploading" };
    setResults((prev) => [...prev, entry]);

    try {
      const formData = new FormData();
      formData.append("file", file);

      const res = await fetch("/api/ingest", {
        method: "POST",
        body: formData,
      });

      if (!res.ok) {
        const text = await res.text().catch(() => res.statusText);
        throw new Error(text || `HTTP ${res.status}`);
      }

      setResults((prev) =>
        prev.map((r) =>
          r.fileName === file.name && r.status === "uploading"
            ? { ...r, status: "success", message: "Ingested successfully" }
            : r
        )
      );
    } catch (err) {
      setResults((prev) =>
        prev.map((r) =>
          r.fileName === file.name && r.status === "uploading"
            ? { ...r, status: "error", message: String(err instanceof Error ? err.message : err) }
            : r
        )
      );
    }
  }, []);

  const handleFiles = useCallback(
    async (files: FileList | File[]) => {
      const pdfFiles = Array.from(files).filter(
        (f) => f.type === "application/pdf" || f.name.endsWith(".pdf")
      );
      if (pdfFiles.length === 0) return;

      setUploading(true);
      for (const file of pdfFiles) {
        await uploadFile(file);
      }
      setUploading(false);
    },
    [uploadFile]
  );

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setDragOver(false);
      if (e.dataTransfer.files.length > 0) {
        handleFiles(e.dataTransfer.files);
      }
    },
    [handleFiles]
  );

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(true);
  }, []);

  const handleDragLeave = useCallback(() => {
    setDragOver(false);
  }, []);

  return (
    <div className="h-full flex flex-col gap-4 max-w-2xl mx-auto py-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Upload Documents</h2>
        <p className="text-sm text-gray-500 mt-1">
          Upload PDF files to ingest into the knowledge base for RAG queries.
        </p>
      </div>

      <div
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onClick={() => fileInputRef.current?.click()}
        className={`flex flex-col items-center justify-center gap-3 rounded-xl border-2 border-dashed p-10 cursor-pointer transition-colors ${
          dragOver
            ? "border-blue-400 bg-blue-50"
            : "border-gray-300 bg-white hover:border-gray-400 hover:bg-gray-50"
        }`}
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="w-10 h-10 text-gray-400"
        >
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
          <polyline points="17 8 12 3 7 8" />
          <line x1="12" y1="3" x2="12" y2="15" />
        </svg>
        <div className="text-center">
          <span className="text-sm font-medium text-gray-700">
            Drop PDF files here or click to browse
          </span>
          <p className="text-xs text-gray-400 mt-1">PDF files only</p>
        </div>
        <input
          ref={fileInputRef}
          type="file"
          accept=".pdf,application/pdf"
          multiple
          className="hidden"
          onChange={(e) => {
            if (e.target.files && e.target.files.length > 0) {
              handleFiles(e.target.files);
              e.target.value = "";
            }
          }}
        />
      </div>

      {uploading && (
        <div className="flex items-center gap-2 text-sm text-blue-600">
          <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
          Uploading...
        </div>
      )}

      {results.length > 0 && (
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium text-gray-700">Upload History</h3>
            <button
              onClick={() => setResults([])}
              className="text-xs text-gray-400 hover:text-gray-600 cursor-pointer"
            >
              Clear
            </button>
          </div>
          {results.map((r, i) => (
            <div
              key={`${r.fileName}-${i}`}
              className={`flex items-center gap-3 rounded-lg border px-4 py-2.5 text-sm ${
                r.status === "success"
                  ? "border-green-200 bg-green-50 text-green-800"
                  : r.status === "error"
                  ? "border-red-200 bg-red-50 text-red-800"
                  : "border-gray-200 bg-white text-gray-600"
              }`}
            >
              <span className="font-medium truncate flex-1">{r.fileName}</span>
              {r.status === "uploading" && <span className="text-xs text-gray-400">Uploading...</span>}
              {r.status === "success" && <span className="text-xs">Ingested</span>}
              {r.status === "error" && (
                <span className="text-xs truncate max-w-[200px]" title={r.message}>
                  {r.message}
                </span>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

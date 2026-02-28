"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const tabs = [
  { href: "/", label: "Chat", activeColor: "text-blue-600 border-blue-600" },
  { href: "/upload", label: "Upload", activeColor: "text-amber-600 border-amber-600" },
];

export function NavBar() {
  const pathname = usePathname();

  return (
    <nav className="h-14 flex items-center gap-6 px-6 border-b border-gray-200 bg-white">
      <span className="font-semibold text-gray-800 mr-4">Company Intelligence</span>
      {tabs.map((tab) => {
        const active = tab.href === "/" ? pathname === "/" : pathname.startsWith(tab.href);
        return (
          <Link
            key={tab.href}
            href={tab.href}
            className={`text-sm pb-0.5 ${
              active
                ? `border-b-2 font-medium ${tab.activeColor}`
                : "text-gray-500 hover:text-gray-700"
            }`}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}

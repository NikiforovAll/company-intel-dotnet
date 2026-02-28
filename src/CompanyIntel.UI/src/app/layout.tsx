import "./globals.css";
import "@copilotkit/react-ui/v2/styles.css";
import type { ReactNode } from "react";

export const metadata = {
  title: "Company Intelligence",
  description: "Company Intelligence Agent",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}

import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Link from "next/link";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Business Loan Application",
  description: "Apply for a small business loan in a couple of minutes.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}>
      <body className="flex min-h-full flex-col font-sans">
        <header className="border-b border-slate-200 bg-white">
          <div className="mx-auto flex max-w-2xl items-center px-4 py-4">
            <Link href="/" className="text-lg font-semibold tracking-tight text-blue-800">
              Business Loans
            </Link>
          </div>
        </header>
        <main className="mx-auto w-full max-w-2xl flex-1 px-4 py-8 sm:py-12">{children}</main>
      </body>
    </html>
  );
}

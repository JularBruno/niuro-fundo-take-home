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
        <header className="bg-emerald-800 text-white shadow-md">
          <div className="mx-auto flex max-w-2xl items-center justify-between gap-4 px-4 py-4">
            <Link
              href="/"
              className="flex items-center gap-2 text-xl font-bold tracking-tight focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-white"
            >
              <span className="flex size-8 items-center justify-center rounded-lg bg-white text-base font-extrabold text-emerald-800" aria-hidden>
                $
              </span>
              Business Loans
            </Link>
            <span className="hidden text-sm text-emerald-100 sm:block">Fast decisions for small businesses</span>
          </div>
        </header>
        <main className="mx-auto w-full max-w-2xl flex-1 px-4 py-8 sm:py-12">{children}</main>
      </body>
    </html>
  );
}

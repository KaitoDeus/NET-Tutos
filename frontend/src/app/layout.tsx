import type { Metadata } from 'next';
import { Inter, JetBrains_Mono } from 'next/font/google';
import './globals.css';
import Navbar from '@/components/Navbar';

const inter = Inter({
  variable: '--font-inter',
  subsets: ['latin'],
});

const jetbrainsMono = JetBrains_Mono({
  variable: '--font-mono',
  subsets: ['latin'],
});

export const metadata: Metadata = {
  title: '.NET Tutos - Nền tảng Học Lập trình .NET 10 & C# 14',
  description: 'Học C# 14, ASP.NET Core 10, Entity Framework Core và thực hành chấm mã Roslyn trực tuyến.',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" className="dark h-full bg-slate-950 text-slate-100 antialiased">
      <body className={`${inter.variable} ${jetbrainsMono.variable} flex min-h-screen flex-col font-sans bg-slate-950`}>
        <Navbar />
        <main className="flex-1">{children}</main>
        <footer className="border-t border-slate-900 bg-slate-950/60 py-6 text-center text-xs text-slate-500">
          <p>© 2026 .NET Tutos Platform. Kiến trúc Decoupled: ASP.NET Core 10 Web API + Next.js 15 React + .NET MAUI.</p>
        </footer>
      </body>
    </html>
  );
}

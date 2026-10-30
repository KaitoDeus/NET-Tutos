import type { Metadata } from 'next';
import './globals.css';
import Navbar from '@/components/Navbar';
import Footer from '@/components/Footer';
import FloatingAiButton from '@/components/FloatingAiButton';

export const metadata: Metadata = {
  title: 'NET-Tutos - Nền tảng Học Lập trình .NET 10 & C# 14',
  description: 'Học C# 14, ASP.NET Core 10, Entity Framework Core và thực hành chấm mã Roslyn trực tuyến.',
  icons: {
    icon: '/images/csharp-logo.png',
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" data-bs-theme="dark" className="h-full">
      <body className="d-flex flex-column min-vh-100">
        <Navbar />
        <main role="main" className="flex-grow-1">
          {children}
        </main>
        <Footer />
        <FloatingAiButton />
      </body>
    </html>
  );
}

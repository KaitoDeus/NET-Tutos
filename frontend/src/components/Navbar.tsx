'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Terminal, BookOpen, Bot, Layers, Flame, Award } from 'lucide-react';

export default function Navbar() {
  const pathname = usePathname();

  const navLinks = [
    { href: '/tutorials', label: 'Bài học', icon: BookOpen },
    { href: '/playground', label: 'C# Sandbox', icon: Terminal },
    { href: '/ai-tutor', label: 'Trợ lý AI', icon: Bot },
    { href: '/flashcards', label: 'Ôn tập Flashcard', icon: Layers },
  ];

  return (
    <header className="sticky top-0 z-50 w-full border-b border-slate-800 bg-slate-950/80 backdrop-blur-md">
      <div className="container mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
        {/* Brand Logo */}
        <Link href="/" className="flex items-center gap-3 group">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 shadow-lg shadow-purple-500/20 group-hover:scale-105 transition-transform">
            <Terminal className="h-5 w-5 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-lg font-bold tracking-tight text-white group-hover:text-purple-400 transition-colors">
                NET-Tutos
              </span>
              <span className="rounded-full bg-purple-500/10 px-2 py-0.5 text-[10px] font-semibold text-purple-400 border border-purple-500/20">
                .NET 10
              </span>
            </div>
            <p className="text-[11px] text-slate-400">Nền tảng Học & Chấm mã C#</p>
          </div>
        </Link>

        {/* Navigation Links */}
        <nav className="hidden md:flex items-center gap-1">
          {navLinks.map((link) => {
            const Icon = link.icon;
            const isActive = pathname.startsWith(link.href);
            return (
              <Link
                key={link.href}
                href={link.href}
                className={`flex items-center gap-2 px-3.5 py-2 rounded-lg text-sm font-medium transition-all ${
                  isActive
                    ? 'bg-purple-600/15 text-purple-300 border border-purple-500/30'
                    : 'text-slate-300 hover:text-white hover:bg-slate-900'
                }`}
              >
                <Icon className={`h-4 w-4 ${isActive ? 'text-purple-400' : 'text-slate-400'}`} />
                {link.label}
              </Link>
            );
          })}
        </nav>

        {/* Right Status */}
        <div className="flex items-center gap-3">
          <div className="hidden sm:flex items-center gap-2 bg-slate-900 border border-slate-800 rounded-full px-3 py-1 text-xs">
            <span className="flex items-center gap-1 text-amber-400 font-semibold">
              <Flame className="h-3.5 w-3.5" /> 5 ngày
            </span>
            <span className="text-slate-600">|</span>
            <span className="flex items-center gap-1 text-purple-400 font-semibold">
              <Award className="h-3.5 w-3.5" /> 420 XP
            </span>
          </div>

          <Link
            href="/playground"
            className="flex items-center gap-2 rounded-lg bg-gradient-to-r from-purple-600 to-indigo-600 px-3.5 py-2 text-xs font-semibold text-white shadow-md shadow-purple-600/20 hover:from-purple-500 hover:to-indigo-500 transition-all"
          >
            <Terminal className="h-3.5 w-3.5" />
            <span>Mở Sandbox</span>
          </Link>
        </div>
      </div>
    </header>
  );
}

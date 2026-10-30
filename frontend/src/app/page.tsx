import Link from 'next/link';
import { Terminal, BookOpen, Bot, Layers, Sparkles, ArrowRight, ShieldCheck, Cpu } from 'lucide-react';
import apiClient from '@/lib/api';
import { Category } from '@/types/api';

export const dynamic = 'force-dynamic';

async function getCategories(): Promise<Category[]> {
  try {
    const res = await apiClient.get<Category[]>('/api/categories');
    return res.data;
  } catch {
    return [
      { id: 1, name: '1. C# & .NET Cơ bản', slug: 'csharp-co-ban', description: 'Cú pháp C#, biến, cấu trúc dữ liệu và logic cơ sở.', badgeColor: 'success', iconClass: 'bi-laptop', tutorialCount: 3 },
      { id: 2, name: '2. OOP & C# Nâng cao', slug: 'oop-csharp-nang-cao', description: '4 trụ cột OOP, Interface, Generics, LINQ và async/await.', badgeColor: 'info', iconClass: 'bi-layers-half', tutorialCount: 4 },
      { id: 3, name: '3. Entity Framework Core & Database', slug: 'entity-framework-core', description: 'ORM hiện đại, LINQ queries, Migrations và Indexing.', badgeColor: 'warning', iconClass: 'bi-database', tutorialCount: 1 },
      { id: 4, name: '4. ASP.NET Core MVC & Web API', slug: 'aspnet-core-mvc-api', description: 'Xây dựng Web REST API, Middleware, DI container.', badgeColor: 'primary', iconClass: 'bi-globe2', tutorialCount: 3 },
    ];
  }
}

export default async function HomePage() {
  const categories = await getCategories();

  return (
    <div className="flex flex-col gap-16 py-10 px-4 sm:px-6 max-w-7xl mx-auto">
      {/* Hero Section */}
      <section className="relative overflow-hidden rounded-3xl border border-slate-800 bg-gradient-to-b from-purple-950/30 via-slate-900/40 to-slate-950 p-8 sm:p-14 text-center">
        <div className="inline-flex items-center gap-2 rounded-full border border-purple-500/30 bg-purple-500/10 px-4 py-1.5 text-xs font-semibold text-purple-300 mb-6 backdrop-blur-sm">
          <Sparkles className="h-3.5 w-3.5 text-purple-400" />
          <span>Hệ sinh thái .NET 10 & C# 14 Hiện Đại Nhất</span>
        </div>

        <h1 className="text-4xl sm:text-6xl font-extrabold tracking-tight text-white max-w-3xl mx-auto leading-tight">
          Làm chủ lập trình <span className="text-transparent bg-clip-text bg-gradient-to-r from-purple-400 via-indigo-300 to-sky-400">.NET & C#</span> từ Zero đến Architect
        </h1>

        <p className="mt-6 text-base sm:text-lg text-slate-300 max-w-2xl mx-auto">
          Hệ thống đào tạo toàn diện: Thực hành chấm mã tức thì với Roslyn Sandbox, giải thích lỗi với AI Tutor và ôn tập phỏng vấn với thuật toán ngắt quãng.
        </p>

        <div className="mt-8 flex flex-wrap items-center justify-center gap-4">
          <Link
            href="/tutorials"
            className="flex items-center gap-2 rounded-xl bg-purple-600 hover:bg-purple-500 px-6 py-3.5 text-sm font-semibold text-white shadow-lg shadow-purple-600/30 transition-all hover:scale-105"
          >
            <BookOpen className="h-4 w-4" />
            <span>Bắt đầu Học Bài Giảng</span>
            <ArrowRight className="h-4 w-4" />
          </Link>

          <Link
            href="/playground"
            className="flex items-center gap-2 rounded-xl border border-slate-700 bg-slate-900/80 hover:bg-slate-800 px-6 py-3.5 text-sm font-semibold text-slate-200 transition-all hover:scale-105"
          >
            <Terminal className="h-4 w-4 text-purple-400" />
            <span>Mở C# Sandbox</span>
          </Link>
        </div>

        {/* Feature Badges */}
        <div className="mt-12 grid grid-cols-2 sm:grid-cols-4 gap-4 border-t border-slate-800/80 pt-8 max-w-3xl mx-auto text-left">
          <div className="flex items-center gap-3">
            <Cpu className="h-5 w-5 text-purple-400 flex-shrink-0" />
            <div>
              <p className="text-xs font-semibold text-white">Roslyn Compiler</p>
              <p className="text-[11px] text-slate-400">Sandbox cách ly an toàn</p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <Bot className="h-5 w-5 text-sky-400 flex-shrink-0" />
            <div>
              <p className="text-xs font-semibold text-white">AI Mentor 24/7</p>
              <p className="text-[11px] text-slate-400">Chẩn đoán lỗi biên dịch</p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <Layers className="h-5 w-5 text-emerald-400 flex-shrink-0" />
            <div>
              <p className="text-xs font-semibold text-white">Spaced Repetition</p>
              <p className="text-[11px] text-slate-400">Hộp nhớ Leitner Box</p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <ShieldCheck className="h-5 w-5 text-amber-400 flex-shrink-0" />
            <div>
              <p className="text-xs font-semibold text-white">Chứng chỉ Xác thực</p>
              <p className="text-[11px] text-slate-400">Mã QR tra cứu công khai</p>
            </div>
          </div>
        </div>
      </section>

      {/* Curriculum Categories Section */}
      <section className="flex flex-col gap-6">
        <div className="flex items-end justify-between">
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-white">Lộ trình bài giảng chuẩn hóa</h2>
            <p className="text-sm text-slate-400 mt-1">Học theo chuỗi module từ cơ bản đến xây dựng hệ thống phân tán</p>
          </div>
          <Link href="/tutorials" className="text-sm font-semibold text-purple-400 hover:text-purple-300 flex items-center gap-1">
            Xem tất cả bài học <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
          {categories.map((cat) => (
            <Link
              key={cat.id}
              href={`/tutorials?category=${cat.slug}`}
              className="group relative flex flex-col justify-between rounded-2xl border border-slate-800 bg-slate-900/60 p-6 hover:border-purple-500/50 hover:bg-slate-900 transition-all hover:-translate-y-1 shadow-sm"
            >
              <div>
                <div className="flex items-center justify-between mb-4">
                  <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-purple-500/10 text-purple-400 border border-purple-500/20 group-hover:scale-110 transition-transform">
                    <BookOpen className="h-5 w-5" />
                  </span>
                  <span className="text-xs font-medium text-slate-400 bg-slate-800/80 px-2.5 py-1 rounded-full border border-slate-700/50">
                    {cat.tutorialCount} bài học
                  </span>
                </div>
                <h3 className="text-base font-bold text-white group-hover:text-purple-300 transition-colors">
                  {cat.name}
                </h3>
                <p className="mt-2 text-xs text-slate-400 line-clamp-3 leading-relaxed">
                  {cat.description}
                </p>
              </div>

              <div className="mt-6 flex items-center gap-1 text-xs font-semibold text-purple-400 group-hover:translate-x-1 transition-transform">
                <span>Khám phá bài học</span>
                <ArrowRight className="h-3 w-3" />
              </div>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}

import Link from 'next/link';
import { BookOpen, Clock, BarChart, ArrowRight, CheckCircle2 } from 'lucide-react';
import apiClient from '@/lib/api';
import { TutorialSummary, Category } from '@/types/api';

export const dynamic = 'force-dynamic';

interface PageProps {
  searchParams: Promise<{ category?: string; search?: string }>;
}

async function getTutorials(category?: string, search?: string): Promise<{ items: TutorialSummary[]; total: number }> {
  try {
    const params: Record<string, string> = {};
    if (category) params.category = category;
    if (search) params.search = search;

    const res = await apiClient.get<{ items: TutorialSummary[]; totalItems: number }>('/api/tutorials', { params });
    return { items: res.data.items || [], total: res.data.totalItems || 0 };
  } catch {
    return { items: [], total: 0 };
  }
}

async function getCategories(): Promise<Category[]> {
  try {
    const res = await apiClient.get<Category[]>('/api/categories');
    return res.data;
  } catch {
    return [];
  }
}

export default async function TutorialsPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const { items: tutorials } = await getTutorials(params.category, params.search);
  const categories = await getCategories();

  return (
    <div className="container mx-auto max-w-7xl px-4 py-10 flex flex-col gap-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-extrabold text-white tracking-tight">Thư viện Bài giảng .NET & C#</h1>
        <p className="text-sm text-slate-400 mt-1">
          Hệ thống bài học biên soạn bài bản từ cơ bản đến nâng cao, bám sát tài liệu chính thức từ Microsoft.
        </p>
      </div>

      {/* Category Tabs */}
      <div className="flex flex-wrap items-center gap-2 border-b border-slate-800 pb-4">
        <Link
          href="/tutorials"
          className={`rounded-xl px-4 py-2 text-xs font-semibold transition-all ${
            !params.category
              ? 'bg-purple-600 text-white shadow-md shadow-purple-600/20'
              : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-white'
          }`}
        >
          Tất cả bài học
        </Link>

        {categories.map((c) => (
          <Link
            key={c.id}
            href={`/tutorials?category=${c.slug}`}
            className={`rounded-xl px-4 py-2 text-xs font-semibold transition-all ${
              params.category === c.slug
                ? 'bg-purple-600 text-white shadow-md shadow-purple-600/20'
                : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-white'
            }`}
          >
            {c.name} ({c.tutorialCount})
          </Link>
        ))}
      </div>

      {/* Tutorials Grid */}
      {tutorials.length === 0 ? (
        <div className="rounded-2xl border border-slate-800 bg-slate-900/40 p-12 text-center text-slate-400">
          <BookOpen className="h-10 w-10 text-slate-600 mx-auto mb-3" />
          <p className="text-sm font-medium">Không tìm thấy bài học nào phù hợp với bộ lọc.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {tutorials.map((t) => (
            <Link
              key={t.id}
              href={`/tutorials/${t.slug}`}
              className="group flex flex-col justify-between rounded-2xl border border-slate-800 bg-slate-900/60 p-6 hover:border-purple-500/50 hover:bg-slate-900 transition-all hover:-translate-y-1 shadow-sm"
            >
              <div>
                <div className="flex items-center justify-between mb-3">
                  <span className="text-[11px] font-bold text-purple-400 bg-purple-500/10 border border-purple-500/20 px-2.5 py-1 rounded-full">
                    {t.categoryName}
                  </span>

                  <span className="flex items-center gap-1 text-[11px] text-slate-400">
                    <BarChart className="h-3 w-3" />
                    {t.difficulty}
                  </span>
                </div>

                <h3 className="text-base font-bold text-white group-hover:text-purple-300 transition-colors leading-snug">
                  {t.title}
                </h3>

                <p className="mt-2.5 text-xs text-slate-400 line-clamp-3 leading-relaxed">
                  {t.summary}
                </p>
              </div>

              <div className="mt-6 flex items-center justify-between pt-4 border-t border-slate-800/80 text-xs">
                <span className="flex items-center gap-1 text-slate-400">
                  <Clock className="h-3.5 w-3.5" />
                  {t.estimatedReadingMinutes} phút đọc
                </span>

                <span className="flex items-center gap-1 font-semibold text-purple-400 group-hover:translate-x-1 transition-transform">
                  {t.isCompleted ? (
                    <span className="flex items-center gap-1 text-emerald-400 font-medium">
                      <CheckCircle2 className="h-3.5 w-3.5" /> Đã hoàn thành
                    </span>
                  ) : (
                    <>
                      <span>Học bài</span>
                      <ArrowRight className="h-3.5 w-3.5" />
                    </>
                  )}
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

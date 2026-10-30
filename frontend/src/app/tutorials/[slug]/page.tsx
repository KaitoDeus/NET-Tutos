'use client';

import { useEffect, useState, use } from 'react';
import Link from 'next/link';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { ArrowLeft, Clock, BarChart, CheckCircle2, ChevronLeft, ChevronRight, HelpCircle, Award } from 'lucide-react';
import apiClient from '@/lib/api';
import { TutorialDetail } from '@/types/api';

export default function TutorialDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const resolvedParams = use(params);
  const [tutorial, setTutorial] = useState<TutorialDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [isCompleted, setIsCompleted] = useState(false);
  const [selectedAnswers, setSelectedAnswers] = useState<Record<number, string>>({});
  const [showExplanations, setShowExplanations] = useState<Record<number, boolean>>({});

  useEffect(() => {
    apiClient.get<TutorialDetail>(`/api/tutorials/${resolvedParams.slug}`)
      .then((res) => {
        setTutorial(res.data);
        setIsCompleted(res.data.isCompleted);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [resolvedParams.slug]);

  const handleToggleComplete = async () => {
    if (!tutorial) return;
    try {
      const res = await apiClient.post(`/api/tutorials/${tutorial.id}/complete`);
      setIsCompleted(res.data.isCompleted);
    } catch {
      alert('Vui lòng đăng nhập để lưu tiến độ hoàn thành bài học!');
    }
  };

  const handleSelectOption = (questionId: number, option: string) => {
    setSelectedAnswers((prev) => ({ ...prev, [questionId]: option }));
    setShowExplanations((prev) => ({ ...prev, [questionId]: true }));
  };

  if (loading) {
    return (
      <div className="container mx-auto max-w-4xl px-4 py-16 text-center text-slate-400">
        <p className="animate-pulse">Đang tải nội dung bài giảng...</p>
      </div>
    );
  }

  if (!tutorial) {
    return (
      <div className="container mx-auto max-w-4xl px-4 py-16 text-center">
        <h2 className="text-xl font-bold text-white mb-2">Không tìm thấy bài học</h2>
        <p className="text-sm text-slate-400 mb-6">Bài học này không tồn tại hoặc đã bị gỡ bỏ.</p>
        <Link href="/tutorials" className="text-sm font-semibold text-purple-400 hover:underline">
          ← Quay lại danh sách bài học
        </Link>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-4xl px-4 py-10 flex flex-col gap-8">
      {/* Back Link */}
      <Link href="/tutorials" className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-400 hover:text-white transition-colors">
        <ArrowLeft className="h-4 w-4" /> Quay lại danh sách bài giảng
      </Link>

      {/* Header Info */}
      <div className="border-b border-slate-800 pb-6">
        <div className="flex items-center gap-3 mb-3">
          <span className="text-xs font-bold text-purple-400 bg-purple-500/10 border border-purple-500/20 px-3 py-1 rounded-full">
            {tutorial.categoryName}
          </span>
          <span className="flex items-center gap-1 text-xs text-slate-400">
            <Clock className="h-3.5 w-3.5" /> {tutorial.estimatedReadingMinutes} phút đọc
          </span>
          <span className="flex items-center gap-1 text-xs text-slate-400">
            <BarChart className="h-3.5 w-3.5" /> {tutorial.difficulty}
          </span>
        </div>

        <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight leading-tight">
          {tutorial.title}
        </h1>

        <p className="mt-3 text-sm text-slate-300 leading-relaxed">
          {tutorial.summary}
        </p>

        {/* Completion Action */}
        <div className="mt-6 flex items-center justify-between pt-4 border-t border-slate-800/80">
          <button
            onClick={handleToggleComplete}
            className={`flex items-center gap-2 rounded-xl px-5 py-2.5 text-xs font-semibold transition-all ${
              isCompleted
                ? 'bg-emerald-600/20 border border-emerald-500/40 text-emerald-300'
                : 'bg-purple-600 hover:bg-purple-500 text-white shadow-md shadow-purple-600/20'
            }`}
          >
            <CheckCircle2 className="h-4 w-4" />
            <span>{isCompleted ? 'Đã hoàn thành bài học' : 'Đánh dấu hoàn thành (+20 XP)'}</span>
          </button>
        </div>
      </div>

      {/* Markdown Content */}
      <article className="prose prose-invert prose-purple max-w-none text-slate-200 leading-relaxed">
        <ReactMarkdown remarkPlugins={[remarkGfm]}>
          {tutorial.contentMarkdown}
        </ReactMarkdown>
      </article>

      {/* Quiz Section */}
      {tutorial.quizzes && tutorial.quizzes.length > 0 && (
        <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900/60 p-6 flex flex-col gap-6">
          <div className="flex items-center gap-2">
            <HelpCircle className="h-5 w-5 text-purple-400" />
            <h2 className="text-lg font-bold text-white">Câu hỏi củng cố kiến thức</h2>
          </div>

          <div className="flex flex-col gap-6">
            {tutorial.quizzes.map((q, idx) => {
              const selected = selectedAnswers[q.id];
              const isCorrect = selected === q.correctOption;
              const answered = showExplanations[q.id];

              return (
                <div key={q.id} className="rounded-xl border border-slate-800 bg-slate-950/80 p-5 flex flex-col gap-3">
                  <h3 className="text-sm font-semibold text-white">
                    Câu {idx + 1}: {q.questionText}
                  </h3>

                  <div className="grid grid-cols-1 gap-2 pt-2">
                    {[
                      { key: 'A', text: q.optionA },
                      { key: 'B', text: q.optionB },
                      { key: 'C', text: q.optionC },
                      { key: 'D', text: q.optionD },
                    ].map((opt) => (
                      <button
                        key={opt.key}
                        onClick={() => handleSelectOption(q.id, opt.key)}
                        className={`text-left rounded-lg border p-3 text-xs transition-all flex items-center justify-between ${
                          selected === opt.key
                            ? opt.key === q.correctOption
                              ? 'border-emerald-500 bg-emerald-500/15 text-emerald-200'
                              : 'border-rose-500 bg-rose-500/15 text-rose-200'
                            : 'border-slate-800 bg-slate-900/60 hover:bg-slate-800 text-slate-300'
                        }`}
                      >
                        <span><strong>{opt.key}.</strong> {opt.text}</span>
                        {answered && opt.key === q.correctOption && (
                          <span className="text-[11px] font-bold text-emerald-400">Đáp án đúng</span>
                        )}
                      </button>
                    ))}
                  </div>

                  {answered && (
                    <div className={`mt-2 rounded-lg p-3 text-xs leading-relaxed ${
                      isCorrect ? 'bg-emerald-950/40 text-emerald-300 border border-emerald-900' : 'bg-rose-950/40 text-rose-300 border border-rose-900'
                    }`}>
                      <p className="font-semibold mb-1">{isCorrect ? '✓ Chính xác!' : '✗ Chưa chính xác!'}</p>
                      <p>{q.explanation}</p>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </section>
      )}

      {/* Previous / Next Lesson Navigation */}
      <div className="flex items-center justify-between border-t border-slate-800 pt-6">
        {tutorial.previousTutorial ? (
          <Link
            href={`/tutorials/${tutorial.previousTutorial.slug}`}
            className="flex items-center gap-2 text-xs font-semibold text-slate-400 hover:text-white transition-colors"
          >
            <ChevronLeft className="h-4 w-4" />
            <span>Bài trước: {tutorial.previousTutorial.title}</span>
          </Link>
        ) : <div />}

        {tutorial.nextTutorial && (
          <Link
            href={`/tutorials/${tutorial.nextTutorial.slug}`}
            className="flex items-center gap-2 text-xs font-semibold text-purple-400 hover:text-purple-300 transition-colors"
          >
            <span>Bài kế tiếp: {tutorial.nextTutorial.title}</span>
            <ChevronRight className="h-4 w-4" />
          </Link>
        )}
      </div>
    </div>
  );
}

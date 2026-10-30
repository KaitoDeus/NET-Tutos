'use client';

import { useState, useEffect } from 'react';
import { Layers, RotateCcw, CheckCircle2, ChevronRight, ChevronLeft, Sparkles, Lightbulb, Zap } from 'lucide-react';
import apiClient from '@/lib/api';

interface Flashcard {
  id: number;
  slug: string;
  topic: string;
  topicName: string;
  difficulty: string;
  questionVi: string;
  questionEn: string;
  answerMarkdownVi: string;
  answerMarkdownEn: string;
  interviewTipVi: string;
  interviewTipEn: string;
  boxLevel: number;
  isMastered: boolean;
}

interface FlashcardTopic {
  topic: string;
  name: string;
  description: string;
  colorClass: string;
  totalCards: number;
  masteredCards: number;
}

export default function FlashcardsPage() {
  const [topics, setTopics] = useState<FlashcardTopic[]>([]);
  const [cards, setCards] = useState<Flashcard[]>([]);
  const [selectedTopic, setSelectedTopic] = useState<string>('');
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [loading, setLoading] = useState(true);
  const [feedback, setFeedback] = useState<string | null>(null);

  useEffect(() => {
    apiClient.get<FlashcardTopic[]>('/api/mobile/flashcards/topics')
      .then((res) => {
        setTopics(res.data);
        if (res.data.length > 0) {
          setSelectedTopic(res.data[0].topic);
        }
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedTopic) return;
    setLoading(true);
    apiClient.get<Flashcard[]>(`/api/mobile/flashcards?topic=${selectedTopic}`)
      .then((res) => {
        setCards(res.data);
        setCurrentIndex(0);
        setIsFlipped(false);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [selectedTopic]);

  const currentCard = cards[currentIndex];

  const handleReview = async (rating: number) => {
    if (!currentCard) return;
    try {
      const res = await apiClient.post('/api/mobile/flashcards/review', {
        flashcardId: currentCard.id,
        rating
      });
      setFeedback(res.data.message || `+${res.data.xpEarned || 15} XP!`);
      setTimeout(() => setFeedback(null), 2500);

      // Move to next card
      if (currentIndex < cards.length - 1) {
        setCurrentIndex(prev => prev + 1);
        setIsFlipped(false);
      }
    } catch {
      alert('Vui lòng đăng nhập để lưu kết quả ôn tập flashcard!');
    }
  };

  return (
    <div className="container mx-auto max-w-4xl px-4 py-10 flex flex-col gap-8">
      {/* Header */}
      <div>
        <div className="inline-flex items-center gap-1.5 rounded-full border border-purple-500/30 bg-purple-500/10 px-3 py-1 text-xs font-semibold text-purple-300 mb-3">
          <Sparkles className="h-3.5 w-3.5 text-purple-400" />
          <span>Thuật toán lặp lại ngắt quãng (Leitner 5 Boxes)</span>
        </div>
        <h1 className="text-3xl font-extrabold text-white tracking-tight">Ôn tập Flashcard Phỏng vấn .NET</h1>
        <p className="text-sm text-slate-400 mt-1">
          Hệ thống thẻ câu hỏi tuyển dụng kỹ thuật C#, CLR, LINQ, Async và Clean Architecture được chắt lọc từ các kỳ phỏng vấn thực tế.
        </p>
      </div>

      {/* Topics Bar */}
      <div className="flex flex-wrap items-center gap-2 border-b border-slate-800 pb-4">
        {topics.map((t) => (
          <button
            key={t.topic}
            onClick={() => setSelectedTopic(t.topic)}
            className={`rounded-xl px-4 py-2 text-xs font-semibold transition-all ${
              selectedTopic === t.topic
                ? 'bg-purple-600 text-white shadow-md shadow-purple-600/20'
                : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-white'
            }`}
          >
            {t.name} ({t.totalCards})
          </button>
        ))}
      </div>

      {/* Flashcard Area */}
      {loading ? (
        <div className="rounded-2xl border border-slate-800 bg-slate-900/40 p-16 text-center text-slate-400">
          <p className="animate-pulse">Đang nạp thẻ ôn tập...</p>
        </div>
      ) : cards.length === 0 ? (
        <div className="rounded-2xl border border-slate-800 bg-slate-900/40 p-16 text-center text-slate-400">
          <Layers className="h-10 w-10 text-slate-600 mx-auto mb-3" />
          <p className="text-sm">Chưa có flashcard nào cho chủ đề này.</p>
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          {/* Card Counter & Progress */}
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>Thẻ {currentIndex + 1} / {cards.length}</span>
            <span className="flex items-center gap-1 text-purple-400 font-semibold">
              <Zap className="h-3.5 w-3.5" /> Hộp nhớ: Cấp {currentCard?.boxLevel || 1}
            </span>
          </div>

          {/* Interactive Flashcard */}
          <div
            onClick={() => setIsFlipped(!isFlipped)}
            className="group relative min-h-[320px] rounded-3xl border border-slate-800 bg-gradient-to-br from-slate-900 via-slate-900/80 to-slate-950 p-8 cursor-pointer shadow-xl hover:border-purple-500/50 transition-all flex flex-col justify-between"
          >
            <div>
              <div className="flex items-center justify-between mb-4">
                <span className="text-xs font-bold text-purple-400 bg-purple-500/10 border border-purple-500/20 px-3 py-1 rounded-full">
                  {currentCard?.topicName}
                </span>

                <span className="text-[11px] text-slate-400 flex items-center gap-1 group-hover:text-purple-300 transition-colors">
                  <RotateCcw className="h-3 w-3" /> Nhấn để lật thẻ
                </span>
              </div>

              {!isFlipped ? (
                <div>
                  <h2 className="text-xl sm:text-2xl font-bold text-white mt-4 leading-snug">
                    {currentCard?.questionVi}
                  </h2>
                  <p className="text-xs text-slate-400 mt-3 italic">
                    {currentCard?.questionEn}
                  </p>
                </div>
              ) : (
                <div className="flex flex-col gap-4">
                  <div className="text-sm text-slate-200 leading-relaxed whitespace-pre-wrap">
                    {currentCard?.answerMarkdownVi}
                  </div>

                  {currentCard?.interviewTipVi && (
                    <div className="rounded-xl border border-amber-900/40 bg-amber-950/20 p-3.5 text-xs text-amber-200 flex items-start gap-2.5">
                      <Lightbulb className="h-4 w-4 text-amber-400 flex-shrink-0 mt-0.5" />
                      <div>
                        <strong className="block text-amber-300 mb-0.5">Mẹo trả lời phỏng vấn:</strong>
                        {currentCard.interviewTipVi}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="text-[11px] text-slate-500 text-center mt-6">
              {!isFlipped ? '👉 Nhấn để xem câu trả lời và mẹo ghi điểm' : '✓ Hãy đánh giá mức độ ghi nhớ bên dưới'}
            </div>
          </div>

          {/* Feedback banner */}
          {feedback && (
            <div className="rounded-xl bg-purple-500/15 border border-purple-500/30 p-3 text-center text-xs font-bold text-purple-300">
              {feedback}
            </div>
          )}

          {/* Review Buttons */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <button
              onClick={() => handleReview(1)}
              className="rounded-xl border border-rose-900/60 bg-rose-950/30 hover:bg-rose-900/50 p-3 text-xs font-semibold text-rose-300 transition-all text-center"
            >
              1. Chưa nhớ
            </button>
            <button
              onClick={() => handleReview(2)}
              className="rounded-xl border border-amber-900/60 bg-amber-950/30 hover:bg-amber-900/50 p-3 text-xs font-semibold text-amber-300 transition-all text-center"
            >
              2. Khó
            </button>
            <button
              onClick={() => handleReview(3)}
              className="rounded-xl border border-blue-900/60 bg-blue-950/30 hover:bg-blue-900/50 p-3 text-xs font-semibold text-blue-300 transition-all text-center"
            >
              3. Tốt
            </button>
            <button
              onClick={() => handleReview(4)}
              className="rounded-xl border border-emerald-900/60 bg-emerald-950/30 hover:bg-emerald-900/50 p-3 text-xs font-semibold text-emerald-300 transition-all text-center"
            >
              4. Rất dễ (+XP)
            </button>
          </div>

          {/* Navigation Controls */}
          <div className="flex items-center justify-between pt-2">
            <button
              onClick={() => {
                if (currentIndex > 0) {
                  setCurrentIndex(prev => prev - 1);
                  setIsFlipped(false);
                }
              }}
              disabled={currentIndex === 0}
              className="flex items-center gap-1 text-xs font-semibold text-slate-400 hover:text-white disabled:opacity-30 transition-colors"
            >
              <ChevronLeft className="h-4 w-4" /> Thẻ trước
            </button>

            <button
              onClick={() => {
                if (currentIndex < cards.length - 1) {
                  setCurrentIndex(prev => prev + 1);
                  setIsFlipped(false);
                }
              }}
              disabled={currentIndex === cards.length - 1}
              className="flex items-center gap-1 text-xs font-semibold text-purple-400 hover:text-purple-300 disabled:opacity-30 transition-colors"
            >
              Thẻ kế tiếp <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

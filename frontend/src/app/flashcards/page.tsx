'use client';

import { useState, useEffect } from 'react';
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

      if (currentIndex < cards.length - 1) {
        setCurrentIndex(prev => prev + 1);
        setIsFlipped(false);
      }
    } catch {
      alert('Vui lòng đăng nhập để lưu kết quả ôn tập flashcard!');
    }
  };

  return (
    <div className="container py-4">
      {/* Header Banner */}
      <div className="card border-0 rounded-4 shadow-sm bg-body p-4 p-md-5 mb-4 position-relative overflow-hidden">
        <div className="position-relative" style={{ maxWidth: '820px' }}>
          <div className="d-flex align-items-center gap-2 mb-2">
            <span className="badge bg-primary text-white rounded-pill px-3 py-1.5 fw-semibold">
              <i className="bi bi-lightning-charge-fill me-1"></i> CHINH PHỤC PHỎNG VẤN
            </span>
            <span className="badge bg-warning-subtle text-warning border border-warning-subtle rounded-pill px-3 py-1.5 fw-semibold">
              <i className="bi bi-clock-history me-1"></i> Spaced Repetition (Leitner)
            </span>
          </div>

          <h1 className="fw-extrabold text-body-emphasis mb-2 display-6">
            Bộ Câu Hỏi Phỏng Vấn .NET & Thẻ Ghi Nhớ
          </h1>

          <p className="text-body-secondary lead fs-6 mb-0">
            Làm chủ kiến thức cốt lõi C#, cơ chế CLR, async/await, tối ưu EF Core và Clean Architecture với thuật toán lặp lại ngắt quãng. Tự tin chinh phục mọi câu hỏi kỹ thuật từ Junior đến Senior.
          </p>
        </div>
      </div>

      {/* Topics Bar */}
      <div className="d-flex flex-wrap gap-2 mb-4 pb-3 border-bottom">
        {topics.map((t) => (
          <button
            key={t.topic}
            onClick={() => setSelectedTopic(t.topic)}
            className={`btn btn-sm rounded-pill px-3 py-1.5 fw-semibold ${
              selectedTopic === t.topic ? 'btn-primary' : 'btn-outline-secondary'
            }`}
          >
            {t.name} ({t.totalCards})
          </button>
        ))}
      </div>

      {/* Flashcard Area */}
      {loading ? (
        <div className="card border-0 rounded-4 bg-body p-5 text-center text-muted shadow-sm">
          <div className="spinner-border text-primary mb-2" role="status"></div>
          <p className="mb-0">Đang nạp thẻ ôn tập...</p>
        </div>
      ) : cards.length === 0 ? (
        <div className="card border-0 rounded-4 bg-body p-5 text-center text-muted shadow-sm">
          <i className="bi bi-card-checklist fs-1 mb-2"></i>
          <p className="mb-0">Chưa có flashcard nào cho chủ đề này.</p>
        </div>
      ) : (
        <div className="d-flex flex-column gap-4" style={{ maxWidth: '820px', margin: '0 auto' }}>
          {/* Card Counter & Progress */}
          <div className="d-flex align-items-center justify-content-between text-muted small">
            <span>Thẻ {currentIndex + 1} / {cards.length}</span>
            <span className="badge bg-primary-subtle text-primary rounded-pill px-3 py-1 fw-bold">
              <i className="bi bi-box-seam me-1"></i>Hộp nhớ: Cấp {currentCard?.boxLevel || 1}
            </span>
          </div>

          {/* Interactive Flashcard */}
          <div
            onClick={() => setIsFlipped(!isFlipped)}
            className="card border rounded-4 p-4 p-md-5 shadow-sm bg-body cursor-pointer text-center position-relative transition-all"
            style={{ minHeight: '340px', cursor: 'pointer' }}
          >
            <div className="d-flex align-items-center justify-content-between mb-3">
              <span className="badge bg-secondary-subtle text-secondary rounded-pill px-2.5 py-1">
                {currentCard?.topicName}
              </span>
              <span className="text-muted small">
                <i className="bi bi-arrow-repeat me-1"></i>Nhấn để lật thẻ
              </span>
            </div>

            <div className="my-auto py-3">
              {!isFlipped ? (
                <div>
                  <h3 className="fw-bold text-body-emphasis mb-3 leading-snug">
                    {currentCard?.questionVi}
                  </h3>
                  <p className="text-muted small fst-italic">
                    {currentCard?.questionEn}
                  </p>
                </div>
              ) : (
                <div className="text-start">
                  <div className="text-body leading-relaxed mb-4 whitespace-pre-wrap">
                    {currentCard?.answerMarkdownVi}
                  </div>

                  {currentCard?.interviewTipVi && (
                    <div className="alert alert-warning border-0 rounded-3 py-2 px-3 small d-flex align-items-start gap-2">
                      <i className="bi bi-lightbulb-fill text-warning flex-shrink-0 mt-1"></i>
                      <div>
                        <strong>Mẹo trả lời phỏng vấn:</strong> {currentCard.interviewTipVi}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="text-muted small mt-3">
              {!isFlipped ? '👉 Nhấn để xem câu trả lời và mẹo ghi điểm' : '✓ Hãy đánh giá mức độ ghi nhớ bên dưới'}
            </div>
          </div>

          {/* Feedback message */}
          {feedback && (
            <div className="alert alert-success py-2 px-3 text-center small fw-bold rounded-pill">
              {feedback}
            </div>
          )}

          {/* Review Buttons */}
          <div className="row g-2">
            <div className="col-3">
              <button
                onClick={() => handleReview(1)}
                className="btn btn-outline-danger w-100 rounded-3 py-2 small fw-semibold"
              >
                1. Chưa nhớ
              </button>
            </div>
            <div className="col-3">
              <button
                onClick={() => handleReview(2)}
                className="btn btn-outline-warning w-100 rounded-3 py-2 small fw-semibold"
              >
                2. Khó
              </button>
            </div>
            <div className="col-3">
              <button
                onClick={() => handleReview(3)}
                className="btn btn-outline-primary w-100 rounded-3 py-2 small fw-semibold"
              >
                3. Tốt
              </button>
            </div>
            <div className="col-3">
              <button
                onClick={() => handleReview(4)}
                className="btn btn-outline-success w-100 rounded-3 py-2 small fw-semibold"
              >
                4. Rất dễ (+XP)
              </button>
            </div>
          </div>

          {/* Navigation Controls */}
          <div className="d-flex align-items-center justify-content-between pt-2">
            <button
              onClick={() => {
                if (currentIndex > 0) {
                  setCurrentIndex(prev => prev - 1);
                  setIsFlipped(false);
                }
              }}
              disabled={currentIndex === 0}
              className="btn btn-sm btn-outline-secondary rounded-pill px-3 py-1.5"
            >
              <i className="bi bi-chevron-left me-1"></i> Thẻ trước
            </button>

            <button
              onClick={() => {
                if (currentIndex < cards.length - 1) {
                  setCurrentIndex(prev => prev + 1);
                  setIsFlipped(false);
                }
              }}
              disabled={currentIndex === cards.length - 1}
              className="btn btn-sm btn-primary rounded-pill px-3 py-1.5"
            >
              Thẻ kế tiếp <i className="bi bi-chevron-right ms-1"></i>
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

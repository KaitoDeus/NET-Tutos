'use client';

import { useEffect, useState, use } from 'react';
import Link from 'next/link';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
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
      <div className="container py-5 text-center text-muted">
        <div className="spinner-border text-primary mb-3" role="status"></div>
        <p>Đang tải nội dung bài giảng...</p>
      </div>
    );
  }

  if (!tutorial) {
    return (
      <div className="container py-5 text-center">
        <h3 className="fw-bold text-body-emphasis mb-2">Không tìm thấy bài học</h3>
        <p className="text-muted mb-4">Bài học này không tồn tại hoặc đã bị di chuyển.</p>
        <Link href="/tutorials" className="btn btn-outline-primary rounded-pill px-4">
          ← Quay lại danh sách bài học
        </Link>
      </div>
    );
  }

  return (
    <div className="container py-4" style={{ maxWidth: '960px' }}>
      {/* Breadcrumb Navigation */}
      <nav aria-label="breadcrumb" className="mb-3">
        <ol className="breadcrumb small">
          <li className="breadcrumb-item"><Link href="/">Trang chủ</Link></li>
          <li className="breadcrumb-item"><Link href="/tutorials">Bài học</Link></li>
          <li className="breadcrumb-item"><Link href={`/tutorials?category=${tutorial.categorySlug}`}>{tutorial.categoryName}</Link></li>
          <li className="breadcrumb-item active" aria-current="page">{tutorial.title}</li>
        </ol>
      </nav>

      {/* Lesson Header Card */}
      <div className="card border-0 shadow-sm rounded-4 bg-body p-4 mb-4">
        <div className="d-flex flex-wrap align-items-center gap-2 mb-2">
          <span className="badge bg-primary-subtle text-primary rounded-pill px-3 py-1 fw-bold">
            {tutorial.categoryName}
          </span>
          <span className="badge bg-secondary-subtle text-secondary rounded-pill px-2.5 py-1">
            <i className="bi bi-clock me-1"></i>{tutorial.estimatedReadingMinutes} phút đọc
          </span>
          <span className="badge bg-info-subtle text-info rounded-pill px-2.5 py-1">
            <i className="bi bi-bar-chart me-1"></i>{tutorial.difficulty}
          </span>
        </div>

        <h1 className="fw-extrabold text-body-emphasis mb-3">{tutorial.title}</h1>
        <p className="text-body-secondary mb-4 leading-relaxed">{tutorial.summary}</p>

        <div className="d-flex align-items-center justify-content-between pt-3 border-top">
          <button
            onClick={handleToggleComplete}
            className={`btn rounded-pill px-4 py-2 fw-semibold d-inline-flex align-items-center gap-2 shadow-sm ${
              isCompleted ? 'btn-success' : 'btn-primary'
            }`}
          >
            <i className="bi bi-check-circle-fill"></i>
            <span>{isCompleted ? 'Đã hoàn thành bài học' : 'Đánh dấu hoàn thành (+20 XP)'}</span>
          </button>
        </div>
      </div>

      {/* Markdown Content Card */}
      <div className="card border-0 shadow-sm rounded-4 bg-body p-4 p-md-5 mb-4">
        <article className="markdown-body leading-relaxed">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>
            {tutorial.contentMarkdown}
          </ReactMarkdown>
        </article>
      </div>

      {/* Quizzes Section */}
      {tutorial.quizzes && tutorial.quizzes.length > 0 && (
        <div className="card border-0 shadow-sm rounded-4 bg-body p-4 mb-4">
          <h4 className="fw-bold text-body-emphasis mb-3 d-flex align-items-center gap-2">
            <i className="bi bi-question-circle-fill text-primary"></i>
            <span>Câu hỏi củng cố kiến thức</span>
          </h4>

          <div className="d-flex flex-column gap-4">
            {tutorial.quizzes.map((q, idx) => {
              const selected = selectedAnswers[q.id];
              const isCorrect = selected === q.correctOption;
              const answered = showExplanations[q.id];

              return (
                <div key={q.id} className="card border rounded-3 p-3 bg-body-tertiary">
                  <h6 className="fw-bold mb-3">
                    Câu {idx + 1}: {q.questionText}
                  </h6>

                  <div className="d-flex flex-column gap-2 mb-2">
                    {[
                      { key: 'A', text: q.optionA },
                      { key: 'B', text: q.optionB },
                      { key: 'C', text: q.optionC },
                      { key: 'D', text: q.optionD },
                    ].map((opt) => (
                      <button
                        key={opt.key}
                        onClick={() => handleSelectOption(q.id, opt.key)}
                        className={`btn text-start p-2.5 rounded-3 border small d-flex align-items-center justify-content-between ${
                          selected === opt.key
                            ? opt.key === q.correctOption
                              ? 'btn-success text-white'
                              : 'btn-danger text-white'
                            : 'btn-light border text-body'
                        }`}
                      >
                        <span><strong>{opt.key}.</strong> {opt.text}</span>
                        {answered && opt.key === q.correctOption && (
                          <span className="badge bg-success-subtle text-success">Đáp án đúng</span>
                        )}
                      </button>
                    ))}
                  </div>

                  {answered && (
                    <div className={`alert mt-2 mb-0 py-2 px-3 small ${isCorrect ? 'alert-success' : 'alert-danger'}`}>
                      <strong>{isCorrect ? '✓ Chính xác!' : '✗ Chưa chính xác!'}</strong> {q.explanation}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Previous / Next Lesson Navigation Card */}
      <div className="d-flex align-items-center justify-content-between p-3 card border-0 shadow-sm rounded-4 bg-body">
        {tutorial.previousTutorial ? (
          <Link
            href={`/tutorials/${tutorial.previousTutorial.slug}`}
            className="btn btn-sm btn-outline-secondary rounded-pill px-3 py-1.5 fw-semibold d-inline-flex align-items-center gap-1"
          >
            <i className="bi bi-chevron-left"></i>
            <span>Bài trước: {tutorial.previousTutorial.title}</span>
          </Link>
        ) : <div />}

        {tutorial.nextTutorial && (
          <Link
            href={`/tutorials/${tutorial.nextTutorial.slug}`}
            className="btn btn-sm btn-primary rounded-pill px-3 py-1.5 fw-semibold d-inline-flex align-items-center gap-1 shadow-sm"
          >
            <span>Bài kế tiếp: {tutorial.nextTutorial.title}</span>
            <i className="bi bi-chevron-right"></i>
          </Link>
        )}
      </div>
    </div>
  );
}

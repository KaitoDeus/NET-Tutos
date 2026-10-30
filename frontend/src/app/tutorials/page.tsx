import Link from 'next/link';
import apiClient from '@/lib/api';
import { TutorialSummary, Category } from '@/types/api';

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
  const { items: tutorials, total } = await getTutorials(params.category, params.search);
  const categories = await getCategories();

  return (
    <div className="container py-4">
      {/* Header */}
      <div className="row align-items-center mb-4">
        <div className="col-md-8">
          <h1 className="fw-bold mb-1 text-body-emphasis">Tất Cả Bài Học .NET</h1>
          <p className="text-muted mb-0">
            Tìm thấy <strong>{total}</strong> bài hướng dẫn chi tiết từ cơ bản đến nâng cao
          </p>
        </div>
        <div className="col-md-4 text-md-end mt-3 mt-md-0">
          <Link href="/playground" className="btn btn-outline-primary rounded-pill px-3 py-1.5 fw-semibold">
            <i className="bi bi-terminal-fill me-1"></i> Thực hành C# Sandbox
          </Link>
        </div>
      </div>

      {/* Filter Tabs */}
      <div className="d-flex flex-wrap gap-2 mb-4 pb-3 border-bottom">
        <Link
          href="/tutorials"
          className={`btn btn-sm rounded-pill px-3 py-1.5 fw-semibold ${
            !params.category ? 'btn-primary' : 'btn-outline-secondary'
          }`}
        >
          Tất cả chuyên đề
        </Link>
        {categories.map((c) => (
          <Link
            key={c.id}
            href={`/tutorials?category=${c.slug}`}
            className={`btn btn-sm rounded-pill px-3 py-1.5 fw-semibold ${
              params.category === c.slug ? 'btn-primary' : 'btn-outline-secondary'
            }`}
          >
            {c.name} ({c.tutorialCount})
          </Link>
        ))}
      </div>

      {/* Tutorials Grid */}
      {tutorials.length === 0 ? (
        <div className="card border-0 rounded-4 bg-body p-5 text-center shadow-sm">
          <i className="bi bi-book fs-1 text-muted mb-2"></i>
          <p className="text-muted mb-0">Không tìm thấy bài học nào phù hợp với bộ lọc.</p>
        </div>
      ) : (
        <div className="row g-4">
          {tutorials.map((t) => (
            <div key={t.id} className="col-md-6 col-lg-4">
              <Link
                href={`/tutorials/${t.slug}`}
                className="card h-100 border rounded-4 text-decoration-none shadow-sm hover-shadow bg-body p-3 transition-all"
              >
                <div className="card-body d-flex flex-column justify-content-between p-2">
                  <div>
                    <div className="d-flex align-items-center justify-content-between mb-2.5">
                      <span className="badge bg-primary-subtle text-primary rounded-pill px-2.5 py-1">
                        {t.categoryName}
                      </span>
                      <span className="badge bg-secondary-subtle text-secondary rounded-pill px-2 py-0.5" style={{ fontSize: '0.7rem' }}>
                        {t.difficulty}
                      </span>
                    </div>

                    <h5 className="fw-bold text-body-emphasis mb-2 line-clamp-2">{t.title}</h5>
                    <p className="text-body-secondary small mb-3 line-clamp-3">{t.summary}</p>
                  </div>

                  <div className="d-flex align-items-center justify-content-between pt-3 border-top text-muted small">
                    <span>
                      <i className="bi bi-clock me-1"></i>{t.estimatedReadingMinutes} phút đọc
                    </span>
                    <span className="text-primary fw-bold">
                      {t.isCompleted ? (
                        <span className="text-success"><i className="bi bi-check-circle-fill me-1"></i>Đã xong</span>
                      ) : (
                        <span>Học bài <i className="bi bi-arrow-right"></i></span>
                      )}
                    </span>
                  </div>
                </div>
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

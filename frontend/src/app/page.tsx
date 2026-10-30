import Link from 'next/link';
import apiClient from '@/lib/api';
import { Category } from '@/types/api';

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
    <div className="container py-4">
      {/* Hero Section (Inspired by Microsoft Learn .NET) */}
      <section className="hero-learn-dotnet mb-5">
        <div className="text-center mb-4 mb-md-5">
          <div className="d-inline-flex align-items-center gap-2 rounded-pill px-3 py-1 bg-primary-subtle border border-primary-subtle text-primary small fw-bold mb-3">
            <i className="bi bi-stars text-warning"></i>
            <span>Hệ sinh thái .NET 10 & C# 14 Hiện Đại Nhất</span>
          </div>

          <h1 className="hero-title fw-extrabold text-gradient-purple mb-2 mb-md-3 display-4">
            Học Lập Trình .NET
          </h1>
          <p className="hero-lead text-body-secondary mx-auto fs-5" style={{ maxWidth: '820px' }}>
            Kho khóa học, video, bài hướng dẫn và lộ trình học tập miễn phí từ cơ bản đến nâng cao dành cho mọi lập trình viên .NET.
          </p>
        </div>

        {/* Spotlight Showcase Card */}
        <div className="row justify-content-center">
          <div className="col-xl-11">
            <div className="card spotlight-card rounded-4 overflow-hidden border shadow-sm">
              <div className="card-body p-3 p-md-4 p-lg-5">
                <div className="row g-4 g-lg-5 align-items-center">
                  {/* Left: Vector Graphic representing What is .NET */}
                  <div className="col-lg-6">
                    <div className="video-preview-wrapper rounded-3 overflow-hidden shadow-sm border" style={{ aspectRatio: '16/9' }}>
                      <svg viewBox="0 0 640 380" className="w-100 h-100" xmlns="http://www.w3.org/2000/svg">
                        <rect width="640" height="380" fill="#1e1e2d" />
                        <defs>
                          <pattern id="grid" width="30" height="30" patternUnits="userSpaceOnUse">
                            <path d="M 30 0 L 0 0 0 30" fill="none" stroke="#2d2d3f" strokeWidth="0.8" />
                          </pattern>
                        </defs>
                        <rect width="640" height="380" fill="url(#grid)" />

                        <rect x="36" y="32" width="60" height="24" rx="4" fill="#0d6efd" />
                        <text x="66" y="48" fontFamily="'Plus Jakarta Sans', sans-serif" fontSize="12" fontWeight="bold" fill="#ffffff" textAnchor="middle">.NET</text>
                        <text x="108" y="48" fontFamily="'Plus Jakarta Sans', sans-serif" fontSize="12" fontWeight="600" fill="#a0aec0">Video giới thiệu .NET</text>

                        <rect x="36" y="62" width="135" height="20" rx="4" fill="#512bd4" />
                        <text x="103" y="76" fontFamily="'Plus Jakarta Sans', sans-serif" fontSize="10" fontWeight="bold" fill="#ffffff" textAnchor="middle">CHO NGƯỜI MỚI</text>

                        <text x="36" y="160" fontFamily="'Plus Jakarta Sans', sans-serif" fontSize="44" fontWeight="800" fill="#ffffff" letterSpacing="-1">.NET là gì?</text>

                        <g transform="translate(460, 40)">
                          <polygon points="40,25 75,5 110,25 75,45" fill="#fde68a" />
                          <polygon points="40,25 75,45 75,85 40,65" fill="#f59e0b" />
                          <polygon points="75,45 110,25 110,65 75,85" fill="#d97706" />
                        </g>

                        <g transform="translate(410, 110)">
                          <polygon points="40,25 75,5 110,25 75,45" fill="#a7f3d0" />
                          <polygon points="40,25 75,45 75,85 40,65" fill="#10b981" />
                          <polygon points="75,45 110,25 110,65 75,85" fill="#059669" />
                        </g>

                        <g transform="translate(480, 180)">
                          <polygon points="40,25 75,5 110,25 75,45" fill="#c4b5fd" />
                          <polygon points="40,25 75,45 75,85 40,65" fill="#8b5cf6" />
                          <polygon points="75,45 110,25 110,65 75,85" fill="#6d28d9" />
                        </g>
                      </svg>
                    </div>
                  </div>

                  {/* Right: Getting Started Action Steps */}
                  <div className="col-lg-6">
                    <div className="d-flex align-items-center gap-2 mb-2">
                      <span className="badge bg-primary text-white rounded-pill px-2.5 py-1 small fw-bold">
                        Bắt đầu
                      </span>
                      <span className="text-body-secondary small">Lộ trình học tập tương tác</span>
                    </div>

                    <h2 className="fw-extrabold text-body-emphasis mb-3">
                      Bắt đầu với .NET hoàn toàn miễn phí
                    </h2>

                    <p className="text-body-secondary mb-4 leading-relaxed">
                      Nền tảng giúp bạn làm chủ hệ sinh thái .NET qua các bài học cô đọng, ví dụ thực chiến và khả năng chạy mã C# trực tiếp trên trình duyệt.
                    </p>

                    <div className="d-flex flex-column gap-3 mb-4">
                      <div className="d-flex align-items-center gap-3">
                        <div className="rounded-circle bg-primary-subtle text-primary p-2 d-flex align-items-center justify-content-center flex-shrink-0" style={{ width: '36px', height: '36px' }}>
                          <i className="bi bi-download"></i>
                        </div>
                        <div className="small"><strong>Bước 1:</strong> Cài đặt .NET 10 SDK và công cụ lập trình VS Code.</div>
                      </div>

                      <div className="d-flex align-items-center gap-3">
                        <div className="rounded-circle bg-success-subtle text-success p-2 d-flex align-items-center justify-content-center flex-shrink-0" style={{ width: '36px', height: '36px' }}>
                          <i className="bi bi-code-slash"></i>
                        </div>
                        <div className="small"><strong>Bước 2:</strong> Viết chương trình C# 14 đầu tiên với Top-Level Statements.</div>
                      </div>

                      <div className="d-flex align-items-center gap-3">
                        <div className="rounded-circle bg-warning-subtle text-warning p-2 d-flex align-items-center justify-content-center flex-shrink-0" style={{ width: '36px', height: '36px' }}>
                          <i className="bi bi-play-circle"></i>
                        </div>
                        <div className="small"><strong>Bước 3:</strong> Chạy code trực tuyến và giải câu đố LeetCode-style.</div>
                      </div>
                    </div>

                    <div className="d-flex flex-wrap gap-2.5">
                      <Link href="/tutorials" className="btn btn-primary rounded-pill px-4 py-2.5 fw-bold shadow-sm d-inline-flex align-items-center gap-2">
                        <i className="bi bi-book"></i>
                        <span>Bắt đầu học ngay</span>
                      </Link>

                      <Link href="/playground" className="btn btn-outline-primary rounded-pill px-4 py-2.5 fw-bold d-inline-flex align-items-center gap-2">
                        <i className="bi bi-terminal-fill"></i>
                        <span>Thực hành Sandbox</span>
                      </Link>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Curriculum Categories Section */}
      <section className="mb-5">
        <div className="d-flex align-items-end justify-content-between mb-4">
          <div>
            <h3 className="fw-extrabold text-body-emphasis mb-1">
              <i className="bi bi-collection-play-fill text-primary me-2"></i>
              Lộ trình Chuyên đề Toàn diện
            </h3>
            <p className="text-body-secondary small mb-0">Học theo chuỗi chuyên đề từ cú pháp cơ sở đến kiến trúc ứng dụng phân tán</p>
          </div>
          <Link href="/tutorials" className="btn btn-sm btn-outline-primary rounded-pill px-3 py-1.5 fw-semibold d-none d-md-inline-flex align-items-center gap-1">
            <span>Xem tất cả bài học</span>
            <i className="bi bi-arrow-right"></i>
          </Link>
        </div>

        <div className="row g-4">
          {categories.map((cat) => (
            <div key={cat.id} className="col-md-6 col-lg-3">
              <Link href={`/tutorials?category=${cat.slug}`} className="card h-100 border rounded-4 text-decoration-none shadow-sm hover-shadow transition-all bg-body p-3">
                <div className="card-body d-flex flex-column justify-content-between p-2">
                  <div>
                    <div className="d-flex align-items-center justify-content-between mb-3">
                      <div className={`rounded-3 bg-${cat.badgeColor}-subtle text-${cat.badgeColor} p-2.5 d-flex align-items-center justify-content-center`} style={{ width: '44px', height: '44px' }}>
                        <i className={`bi ${cat.iconClass} fs-5`}></i>
                      </div>
                      <span className={`badge bg-${cat.badgeColor}-subtle text-${cat.badgeColor} rounded-pill px-2.5 py-1`}>
                        {cat.tutorialCount} bài học
                      </span>
                    </div>

                    <h5 className="fw-bold text-body-emphasis mb-2">{cat.name}</h5>
                    <p className="text-body-secondary small mb-0 line-clamp-3">{cat.description}</p>
                  </div>

                  <div className="mt-4 pt-3 border-top d-flex align-items-center justify-content-between text-primary small fw-bold">
                    <span>Khám phá ngay</span>
                    <i className="bi bi-arrow-right"></i>
                  </div>
                </div>
              </Link>
            </div>
          ))}
        </div>
      </section>

      {/* Feature Highlights Grid */}
      <section className="mb-5 py-4 border-top">
        <div className="text-center mb-5">
          <h3 className="fw-extrabold text-body-emphasis mb-2">Tại sao nên học .NET tại NET-Tutos?</h3>
          <p className="text-body-secondary small mx-auto" style={{ maxWidth: '640px' }}>
            Môi trường học tập thông minh kết hợp trình biên dịch Roslyn, trợ lý AI và thuật toán Spaced Repetition.
          </p>
        </div>

        <div className="row g-4">
          <div className="col-md-6 col-lg-3">
            <div className="card border rounded-4 bg-body p-4 text-center h-100 shadow-xs">
              <div className="rounded-circle bg-primary-subtle text-primary mx-auto mb-3 p-3 d-flex align-items-center justify-content-center" style={{ width: '56px', height: '56px' }}>
                <i className="bi bi-cpu fs-3"></i>
              </div>
              <h5 className="fw-bold mb-2">Roslyn Compiler</h5>
              <p className="text-body-secondary small mb-0">Biên dịch mã C# 14 thực thi an toàn trong Sandbox cách ly với độ trễ dưới 150ms.</p>
            </div>
          </div>

          <div className="col-md-6 col-lg-3">
            <div className="card border rounded-4 bg-body p-4 text-center h-100 shadow-xs">
              <div className="rounded-circle bg-warning-subtle text-warning mx-auto mb-3 p-3 d-flex align-items-center justify-content-center" style={{ width: '56px', height: '56px' }}>
                <i className="bi bi-robot fs-3"></i>
              </div>
              <h5 className="fw-bold mb-2">AI Mentor 24/7</h5>
              <p className="text-body-secondary small mb-0">Chẩn đoán lỗi biên dịch, phân tích độ phức tạp thuật toán và giải thích chuyên sâu.</p>
            </div>
          </div>

          <div className="col-md-6 col-lg-3">
            <div className="card border rounded-4 bg-body p-4 text-center h-100 shadow-xs">
              <div className="rounded-circle bg-success-subtle text-success mx-auto mb-3 p-3 d-flex align-items-center justify-content-center" style={{ width: '56px', height: '56px' }}>
                <i className="bi bi-card-checklist fs-3"></i>
              </div>
              <h5 className="fw-bold mb-2">Spaced Repetition</h5>
              <p className="text-body-secondary small mb-0">Hộp nhớ Leitner 5 cấp giúp ghi nhớ lâu bền các khái niệm phỏng vấn kỹ thuật quan trọng.</p>
            </div>
          </div>

          <div className="col-md-6 col-lg-3">
            <div className="card border rounded-4 bg-body p-4 text-center h-100 shadow-xs">
              <div className="rounded-circle bg-info-subtle text-info mx-auto mb-3 p-3 d-flex align-items-center justify-content-center" style={{ width: '56px', height: '56px' }}>
                <i className="bi bi-award fs-3"></i>
              </div>
              <h5 className="fw-bold mb-2">Chứng chỉ Số hóa</h5>
              <p className="text-body-secondary small mb-0">Cấp chứng nhận hoàn thành khóa học kèm mã xác thực QR tra cứu công khai trực tuyến.</p>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

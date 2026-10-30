'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

export default function Navbar() {
  const pathname = usePathname();
  const [theme, setTheme] = useState<'light' | 'dark'>('dark');
  const [lang, setLang] = useState<'vi' | 'en'>('vi');
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);

  useEffect(() => {
    const savedTheme = (localStorage.getItem('theme') as 'light' | 'dark') || 'dark';
    setTheme(savedTheme);
    document.documentElement.setAttribute('data-bs-theme', savedTheme);
  }, []);

  const toggleTheme = () => {
    const nextTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(nextTheme);
    document.documentElement.setAttribute('data-bs-theme', nextTheme);
    localStorage.setItem('theme', nextTheme);
  };

  const isEn = lang === 'en';

  return (
    <header className="sticky-top">
      <nav className="navbar navbar-expand-lg border-bottom shadow-sm bg-body">
        <div className="container-fluid px-3 px-xl-4">
          {/* Brand Logo */}
          <Link href="/" className="navbar-brand d-flex align-items-center gap-2 fw-bold text-decoration-none py-1 me-2 me-sm-3">
            <img src="/images/csharp-logo.png" alt="C# Logo" width="30" height="34" className="csharp-nav-logo" />
            <span className="brand-text fw-extrabold fs-4">
              <span className="text-dotnet">.NET</span>
              <span className="text-body-emphasis"> Tutos</span>
            </span>
          </Link>

          {/* Mobile Quick Action Buttons */}
          <div className="d-flex align-items-center gap-2 d-lg-none ms-auto">
            <button
              onClick={() => setLang(lang === 'vi' ? 'en' : 'vi')}
              className="btn btn-sm lang-switcher-btn rounded-circle p-0"
              style={{ width: '32px', height: '32px' }}
              title={isEn ? "Language" : "Ngôn ngữ"}
            >
              <img src={isEn ? "/images/flags/en.svg" : "/images/flags/vn.svg"} alt="Flag" style={{ width: '22px', height: '22px' }} />
            </button>

            <button
              onClick={toggleTheme}
              className="btn btn-sm btn-outline-secondary rounded-circle p-1.5"
              style={{ width: '32px', height: '32px' }}
              title={isEn ? "Toggle theme" : "Chuyển chế độ Sáng / Tối"}
            >
              <i className={`bi ${theme === 'dark' ? 'bi-brightness-high-fill text-warning' : 'bi-moon-stars-fill'}`}></i>
            </button>

            <button
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className="navbar-toggler border-0 p-1.5 rounded-3 shadow-none bg-body-tertiary"
              type="button"
            >
              <span className="navbar-toggler-icon" style={{ width: '1.25em', height: '1.25em' }}></span>
            </button>
          </div>

          {/* Desktop Navigation Links */}
          <div className={`collapse navbar-collapse ${isMobileMenuOpen ? 'show' : ''}`} id="navbarContent">
            <ul className="navbar-nav me-auto mb-2 mb-lg-0 ms-lg-2 gap-1 align-items-lg-center">
              {/* 1. Trang chủ */}
              <li className="nav-item">
                <Link href="/" className={`nav-link text-nowrap ${pathname === '/' ? 'active fw-semibold' : ''}`}>
                  <i className="bi bi-house-door me-1"></i> {isEn ? "Home" : "Trang chủ"}
                </Link>
              </li>

              {/* 2. Khóa học & Lộ trình Dropdown */}
              <li className="nav-item dropdown position-relative" onMouseLeave={() => setOpenDropdown(null)}>
                <button
                  onClick={() => setOpenDropdown(openDropdown === 'courses' ? null : 'courses')}
                  className={`nav-link dropdown-toggle text-nowrap btn btn-link text-decoration-none ${pathname.startsWith('/tutorials') ? 'active fw-semibold' : ''}`}
                >
                  <i className="bi bi-grid-fill me-1 text-primary"></i> {isEn ? "Courses & Roadmap" : "Khóa học & Lộ trình"}
                </button>
                {openDropdown === 'courses' && (
                  <div className="dropdown-menu shadow-lg border-0 rounded-4 p-3 show position-absolute" style={{ width: '320px', zIndex: 1050 }}>
                    <div className="d-flex flex-column gap-1">
                      <Link href="/tutorials" onClick={() => setOpenDropdown(null)} className="dropdown-item category-item-card p-2 rounded-3 d-flex align-items-center gap-2.5">
                        <div className="category-icon-box bg-primary-subtle text-primary p-2 rounded-2">
                          <i className="bi bi-book"></i>
                        </div>
                        <div>
                          <div className="category-text-title fw-bold small">{isEn ? "Tutorials Library" : "Thư viện bài học"}</div>
                          <div className="category-text-desc text-muted" style={{ fontSize: '0.75rem' }}>Kho tàng bài giảng C#/.NET 10</div>
                        </div>
                      </Link>

                      <Link href="/tutorials?category=csharp-co-ban" onClick={() => setOpenDropdown(null)} className="dropdown-item category-item-card p-2 rounded-3 d-flex align-items-center gap-2.5">
                        <div className="category-icon-box bg-success-subtle text-success p-2 rounded-2">
                          <i className="bi bi-laptop"></i>
                        </div>
                        <div>
                          <div className="category-text-title fw-bold small">1. C# & .NET Cơ bản</div>
                          <div className="category-text-desc text-muted" style={{ fontSize: '0.75rem' }}>Cú pháp, biến, luồng điều khiển</div>
                        </div>
                      </Link>

                      <Link href="/tutorials?category=oop-csharp-nang-cao" onClick={() => setOpenDropdown(null)} className="dropdown-item category-item-card p-2 rounded-3 d-flex align-items-center gap-2.5">
                        <div className="category-icon-box bg-info-subtle text-info p-2 rounded-2">
                          <i className="bi bi-layers-half"></i>
                        </div>
                        <div>
                          <div className="category-text-title fw-bold small">2. OOP & C# Nâng cao</div>
                          <div className="category-text-desc text-muted" style={{ fontSize: '0.75rem' }}>SOLID, LINQ, Async/Await</div>
                        </div>
                      </Link>
                    </div>
                  </div>
                )}
              </li>

              {/* 3. Thực hành & Dự án Dropdown */}
              <li className="nav-item dropdown position-relative" onMouseLeave={() => setOpenDropdown(null)}>
                <button
                  onClick={() => setOpenDropdown(openDropdown === 'practice' ? null : 'practice')}
                  className={`nav-link dropdown-toggle text-nowrap btn btn-link text-decoration-none ${pathname === '/playground' || pathname === '/ai-tutor' || pathname === '/flashcards' ? 'active fw-semibold' : ''}`}
                >
                  <i className="bi bi-code-slash me-1 text-success"></i> {isEn ? "Practice & AI" : "Thực hành & Dự án"}
                </button>
                {openDropdown === 'practice' && (
                  <div className="dropdown-menu shadow-lg border-0 rounded-4 p-3 show position-absolute" style={{ width: '320px', zIndex: 1050 }}>
                    <div className="d-flex flex-column gap-1">
                      <Link href="/playground" onClick={() => setOpenDropdown(null)} className="dropdown-item category-item-card p-2 rounded-3 d-flex align-items-center gap-2.5">
                        <div className="category-icon-box bg-primary-subtle text-primary p-2 rounded-2">
                          <i className="bi bi-terminal-fill"></i>
                        </div>
                        <div>
                          <div className="category-text-title fw-bold small">{isEn ? "C# Sandbox" : "C# Code Playground"}</div>
                          <div className="category-text-desc text-muted" style={{ fontSize: '0.75rem' }}>Biên dịch & Chạy mã Roslyn trực tuyến</div>
                        </div>
                      </Link>

                      <Link href="/ai-tutor" onClick={() => setOpenDropdown(null)} className="dropdown-item category-item-card p-2 rounded-3 d-flex align-items-center gap-2.5">
                        <div className="category-icon-box bg-warning-subtle text-warning p-2 rounded-2">
                          <i className="bi bi-robot"></i>
                        </div>
                        <div>
                          <div className="category-text-title fw-bold small">{isEn ? "AI .NET Tutor" : "Trợ Lý AI Gia Sư .NET"}</div>
                          <div className="category-text-desc text-muted" style={{ fontSize: '0.75rem' }}>Hỏi đáp & Giải thích mã lỗi biên dịch</div>
                        </div>
                      </Link>

                      <Link href="/flashcards" onClick={() => setOpenDropdown(null)} className="dropdown-item category-item-card p-2 rounded-3 d-flex align-items-center gap-2.5">
                        <div className="category-icon-box bg-danger-subtle text-danger p-2 rounded-2">
                          <i className="bi bi-card-checklist"></i>
                        </div>
                        <div>
                          <div className="category-text-title fw-bold small">{isEn ? "Interview Flashcards" : "Ôn tập Flashcard Phỏng vấn"}</div>
                          <div className="category-text-desc text-muted" style={{ fontSize: '0.75rem' }}>Luyện câu hỏi tuyển dụng C# / .NET</div>
                        </div>
                      </Link>
                    </div>
                  </div>
                )}
              </li>
            </ul>

            {/* Right Nav Controls */}
            <div className="d-flex align-items-center gap-2 ms-lg-auto">
              {/* Streak Badge */}
              <div className="d-none d-sm-flex align-items-center gap-1.5 px-3 py-1 rounded-pill bg-warning-subtle text-warning border border-warning-subtle small fw-bold">
                <i className="bi bi-fire text-danger"></i>
                <span>5 ngày</span>
              </div>

              {/* Language Switcher */}
              <button
                onClick={() => setLang(lang === 'vi' ? 'en' : 'vi')}
                className="btn btn-sm lang-switcher-btn rounded-circle p-0 d-none d-lg-flex align-items-center justify-content-center"
                style={{ width: '32px', height: '32px' }}
                title={isEn ? "Switch to Vietnamese" : "Chuyển sang Tiếng Anh"}
              >
                <img src={isEn ? "/images/flags/en.svg" : "/images/flags/vn.svg"} alt="Flag" width="22" height="22" />
              </button>

              {/* Theme Toggle Button */}
              <button
                id="themeToggleBtn"
                onClick={toggleTheme}
                className="btn btn-sm btn-outline-secondary rounded-circle theme-btn p-1.5 d-none d-lg-flex align-items-center justify-content-center"
                style={{ width: '32px', height: '32px' }}
                title={isEn ? "Toggle theme" : "Chuyển chế độ Sáng / Tối"}
              >
                <i id="themeIcon" className={`bi ${theme === 'dark' ? 'bi-brightness-high-fill text-warning' : 'bi-moon-stars-fill'}`}></i>
              </button>

              {/* Sandbox Quick Button */}
              <Link
                href="/playground"
                className="btn btn-sm btn-primary rounded-pill px-3 py-1.5 fw-semibold d-inline-flex align-items-center gap-1.5 shadow-sm text-nowrap"
              >
                <i className="bi bi-terminal-fill"></i>
                <span>{isEn ? "C# Sandbox" : "Mở Sandbox"}</span>
              </Link>
            </div>
          </div>
        </div>
      </nav>
    </header>
  );
}

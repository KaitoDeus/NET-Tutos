'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

export default function FloatingAiButton() {
  const pathname = usePathname();

  if (pathname === '/ai-tutor') {
    return null;
  }

  return (
    <div className="floating-ai-container" style={{ position: 'fixed', bottom: '24px', right: '24px', zIndex: 1070 }}>
      <Link
        href="/ai-tutor"
        className="btn btn-primary rounded-circle shadow-lg p-0 d-flex align-items-center justify-content-center floating-ai-btn position-relative text-decoration-none"
        title="Trợ lý AI Gia sư .NET - Hỏi đáp & Sửa lỗi!"
        style={{ width: '56px', height: '56px' }}
      >
        <i className="bi bi-robot fs-4 text-white"></i>
        <span className="position-absolute top-0 start-0 translate-middle badge rounded-pill bg-danger border border-white shadow-sm floating-ai-badge">
          AI
        </span>
      </Link>
    </div>
  );
}

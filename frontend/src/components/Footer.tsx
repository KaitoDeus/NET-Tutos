export default function Footer() {
  return (
    <footer className="footer mt-auto py-4 bg-body-tertiary border-top">
      <div className="container">
        <div className="row g-4 align-items-center">
          <div className="col-md-6 text-center text-md-start">
            <div className="d-flex align-items-center justify-content-center justify-content-md-start gap-2 mb-0">
              <img src="/images/csharp-logo.png" alt="C# Logo" width="24" height="27" style={{ objectFit: 'contain' }} />
              <span className="badge bg-primary text-white p-1 px-2 rounded">.NET 10</span>
              <span className="fw-bold">.NET Tutos</span>
            </div>
          </div>
          <div className="col-md-6 text-center text-md-end">
            <p className="text-muted small mb-0">
              © {new Date().getFullYear()} .NET Tutos. Nền tảng học tập & thực hành lập trình .NET chất lượng cao.
            </p>
          </div>
        </div>
      </div>
    </footer>
  );
}

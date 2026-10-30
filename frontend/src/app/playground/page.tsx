'use client';

import { useState, useEffect } from 'react';
import Editor from '@monaco-editor/react';
import apiClient from '@/lib/api';
import { CodeTemplate, CodeExecutionResponse, CodingChallenge, ChallengeSubmissionResponse } from '@/types/api';

const DEFAULT_CODE = `// C# 14 & .NET 10 Interactive Roslyn Sandbox
Console.WriteLine("Xin chào từ .NET 10 Web API & Next.js!");

var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
var evenSquares = numbers
    .Where(n => n % 2 == 0)
    .Select(n => new { Number = n, Square = n * n });

Console.WriteLine("\\nCác số chẵn và bình phương tương ứng:");
foreach (var item in evenSquares)
{
    Console.WriteLine($" -> {item.Number}^2 = {item.Square}");
}
`;

export default function PlaygroundPage() {
  const [code, setCode] = useState(DEFAULT_CODE);
  const [output, setOutput] = useState<string>('Sẵn sàng thực thi mã nguồn C#.\nNhấn "Chạy code (Ctrl+Enter)" để biên dịch.');
  const [isRunning, setIsRunning] = useState(false);
  const [executionTime, setExecutionTime] = useState<number | null>(null);
  const [templates, setTemplates] = useState<CodeTemplate[]>([]);
  const [challenges, setChallenges] = useState<CodingChallenge[]>([]);
  const [activeTab, setActiveTab] = useState<'editor' | 'challenges'>('editor');
  const [selectedChallenge, setSelectedChallenge] = useState<CodingChallenge | null>(null);

  useEffect(() => {
    apiClient.get<CodeTemplate[]>('/api/playground/templates')
      .then(res => setTemplates(res.data))
      .catch(() => {});

    apiClient.get<CodingChallenge[]>('/api/playground/challenges')
      .then(res => setChallenges(res.data))
      .catch(() => {});
  }, []);

  const handleRunCode = async () => {
    setIsRunning(true);
    setOutput('Đang biên dịch và thực thi mã qua Roslyn Engine...');
    setExecutionTime(null);

    try {
      const res = await apiClient.post<CodeExecutionResponse>('/api/playground/run', { code });
      const data = res.data;
      if (data.isSuccess) {
        setOutput(data.output || '(Chương trình thực thi thành công nhưng không có dữ liệu xuất ra Console)');
        setExecutionTime(data.executionTimeMs);
      } else {
        setOutput(data.error || 'Lỗi biên dịch hoặc thực thi.');
        setExecutionTime(data.executionTimeMs);
      }
    } catch {
      setOutput('Lỗi kết nối tới Backend API.');
    } finally {
      setIsRunning(false);
    }
  };

  const handleSelectTemplate = (templateId: string) => {
    const tmpl = templates.find(t => t.id === templateId);
    if (tmpl) {
      setCode(tmpl.code);
      setSelectedChallenge(null);
      setOutput(`Đã nạp mẫu: ${tmpl.title}\n${tmpl.description}`);
    }
  };

  const handleSelectChallenge = (c: CodingChallenge) => {
    setSelectedChallenge(c);
    setCode(c.initialCode || `// Giải thử thách: ${c.title}\n`);
    setActiveTab('editor');
    setOutput(`🎯 THỬ THÁCH: ${c.title} (+${c.xpReward} XP)\n\n${c.shortDescription}\n\nHãy hoàn thành mã nguồn và nhấn "Nộp bài chấm điểm"!`);
  };

  const handleSubmitChallenge = async () => {
    if (!selectedChallenge) return;
    setIsRunning(true);
    setOutput(`Đang chấm điểm bài tập qua bộ ${selectedChallenge.totalTestCases} TestCases...`);

    try {
      const res = await apiClient.post<ChallengeSubmissionResponse>('/api/playground/submit', {
        challengeId: selectedChallenge.id,
        code
      });
      const data = res.data;
      if (data.allPassed) {
        setOutput(`🎉 CHÚC MỪNG BẠN ĐÃ VƯỢT QUA TẤT CẢ TESTCASES!\nThời gian chạy: ${data.executionTimeMs}ms\nSố test đúng: ${data.passedTestsCount}/${data.totalTestsCount}\nBạn nhận được: +${data.xpEarned || selectedChallenge.xpReward} XP!`);
      } else {
        setOutput(`❌ CHƯA ĐẠT: Đã vượt qua ${data.passedTestsCount}/${data.totalTestsCount} TestCases.\n${data.compileError || 'Hãy kiểm tra lại điều kiện biên trong code!'}`);
      }
    } catch {
      setOutput('Lỗi khi nộp bài chấm điểm. Vui lòng thử lại!');
    } finally {
      setIsRunning(false);
    }
  };

  return (
    <div className="container-fluid px-3 px-xl-4 py-4">
      {/* Header Title & Mode Switcher */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-3 mb-4">
        <div>
          <div className="d-flex align-items-center gap-2 mb-1">
            <span className="badge bg-primary-subtle border border-primary-subtle rounded-pill px-3 py-1 fw-bold text-primary">
              <i className="bi bi-terminal-fill me-1"></i> Interactive C# Runner (.NET 10)
            </span>
            {selectedChallenge && (
              <span className="badge bg-warning-subtle border border-warning-subtle rounded-pill px-3 py-1 fw-bold text-warning">
                ⚡ Thử thách (+{selectedChallenge.xpReward} XP)
              </span>
            )}
          </div>
          <h3 className="fw-bold mb-0 text-body-emphasis">
            {selectedChallenge ? selectedChallenge.title : "Trình Thực Hành Code C# Trực Tuyến"}
          </h3>
        </div>

        <div className="d-flex flex-wrap align-items-center gap-2">
          <button
            onClick={() => setActiveTab(activeTab === 'editor' ? 'challenges' : 'editor')}
            className={`btn rounded-pill px-3 py-2 fw-semibold ${activeTab === 'challenges' ? 'btn-primary' : 'btn-outline-primary'}`}
          >
            <i className="bi bi-list-task me-1"></i> Kho thử thách ({challenges.length} bài)
          </button>
          {selectedChallenge && (
            <button
              onClick={() => {
                setSelectedChallenge(null);
                setCode(DEFAULT_CODE);
                setActiveTab('editor');
                setOutput('Đã trở về chế độ Playground tự do.');
              }}
              className="btn btn-outline-secondary rounded-pill px-3 py-2"
            >
              <i className="bi bi-code-slash me-1"></i> Playground tự do
            </button>
          )}
        </div>
      </div>

      {activeTab === 'challenges' ? (
        <div className="card border-0 shadow-sm rounded-4 bg-body p-4">
          <h4 className="fw-bold mb-3 d-flex align-items-center gap-2 text-body-emphasis">
            <i className="bi bi-trophy-fill text-warning"></i>
            <span>Kho Thử Thách Thuật Toán C#</span>
          </h4>
          <p className="text-body-secondary small mb-4">Luyện tập tư duy giải quyết vấn đề và nhận thưởng XP trực tiếp vào hồ sơ cá nhân.</p>

          <div className="row g-3">
            {challenges.map((c) => (
              <div key={c.id} className="col-md-6 col-lg-4">
                <div className="card h-100 border rounded-4 bg-body p-3 shadow-xs d-flex flex-column justify-content-between">
                  <div>
                    <div className="d-flex align-items-center justify-content-between mb-2">
                      <span className="badge bg-primary-subtle text-primary rounded-pill">{c.categoryName}</span>
                      <span className="badge bg-secondary-subtle text-secondary rounded-pill">{c.difficulty}</span>
                    </div>
                    <h6 className="fw-bold text-body-emphasis mb-2">{c.title}</h6>
                    <p className="text-body-secondary small mb-3 line-clamp-2">{c.shortDescription}</p>
                  </div>

                  <div className="d-flex align-items-center justify-content-between pt-2 border-top">
                    <span className="badge bg-warning-subtle text-warning fw-bold">+{c.xpReward} XP</span>
                    <button
                      onClick={() => handleSelectChallenge(c)}
                      className="btn btn-sm btn-primary rounded-pill px-3 py-1"
                    >
                      Bắt đầu làm →
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      ) : (
        <div className="row g-4">
          {/* Left Column: Monaco Code Editor */}
          <div className="col-lg-7 col-xl-8">
            <div className="card border-0 shadow-sm rounded-4 bg-body overflow-hidden">
              <div className="card-header bg-body border-bottom p-3 d-flex flex-wrap align-items-center justify-content-between gap-2">
                <div className="d-flex align-items-center gap-2">
                  <select
                    className="form-select form-select-sm rounded-pill border-secondary-subtle"
                    style={{ width: '220px' }}
                    onChange={(e) => handleSelectTemplate(e.target.value)}
                    defaultValue=""
                  >
                    <option value="" disabled>-- Chọn mẫu code C# --</option>
                    {templates.map(t => (
                      <option key={t.id} value={t.id}>{t.title}</option>
                    ))}
                  </select>
                </div>

                <div className="d-flex align-items-center gap-2">
                  <button
                    onClick={() => setCode(selectedChallenge ? selectedChallenge.initialCode : DEFAULT_CODE)}
                    className="btn btn-sm btn-outline-secondary rounded-pill px-2.5 py-1"
                    title="Khôi phục code ban đầu"
                  >
                    <i className="bi bi-arrow-counterclockwise"></i>
                  </button>

                  {selectedChallenge ? (
                    <button
                      onClick={handleSubmitChallenge}
                      disabled={isRunning}
                      className="btn btn-sm btn-success rounded-pill px-3 py-1.5 fw-bold d-inline-flex align-items-center gap-1.5 shadow-sm"
                    >
                      <i className="bi bi-check-circle-fill"></i>
                      <span>{isRunning ? "Đang chấm..." : "Nộp bài chấm điểm"}</span>
                    </button>
                  ) : (
                    <button
                      onClick={handleRunCode}
                      disabled={isRunning}
                      className="btn btn-sm btn-primary rounded-pill px-3 py-1.5 fw-bold d-inline-flex align-items-center gap-1.5 shadow-sm"
                    >
                      <i className="bi bi-play-fill fs-6"></i>
                      <span>{isRunning ? "Đang chạy..." : "Chạy code (Ctrl+Enter)"}</span>
                    </button>
                  )}
                </div>
              </div>

              <div className="monaco-container" style={{ height: '520px' }}>
                <Editor
                  height="100%"
                  language="csharp"
                  theme="vs-dark"
                  value={code}
                  onChange={(val) => setCode(val || '')}
                  options={{
                    fontSize: 14,
                    fontFamily: 'var(--font-mono), Consolas, "Courier New", monospace',
                    minimap: { enabled: false },
                    scrollBeyondLastLine: false,
                    automaticLayout: true,
                    tabSize: 4,
                  }}
                />
              </div>
            </div>
          </div>

          {/* Right Column: Console Terminal Output */}
          <div className="col-lg-5 col-xl-4">
            <div className="card border-0 shadow-sm rounded-4 bg-body overflow-hidden h-100 d-flex flex-column">
              <div className="card-header bg-body border-bottom p-3 d-flex align-items-center justify-content-between">
                <div className="d-flex align-items-center gap-2 fw-bold small text-body-emphasis">
                  <i className="bi bi-terminal me-1 text-primary"></i>
                  <span>Kết quả thực thi (Console Output)</span>
                </div>
                {executionTime !== null && (
                  <span className="badge bg-secondary-subtle text-secondary small">
                    <i className="bi bi-clock me-1"></i>{executionTime}ms
                  </span>
                )}
              </div>

              <div className="card-body p-3 flex-grow-1 d-flex flex-column">
                <div className="console-terminal flex-grow-1" id="consoleOutput" style={{ minHeight: '440px' }}>
                  {output}
                </div>

                <div className="d-flex align-items-center justify-content-between mt-3 text-muted small" style={{ fontSize: '0.75rem' }}>
                  <span><i className="bi bi-cpu me-1"></i>Roslyn C# Sandbox</span>
                  <span>Exit Code: 0</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

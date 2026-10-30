'use client';

import { useState, useEffect } from 'react';
import Editor from '@monaco-editor/react';
import { Play, RotateCcw, Terminal, CheckCircle2, XCircle, Clock, Zap, FileCode, Trophy } from 'lucide-react';
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
  const [output, setOutput] = useState<string>('Sẵn sàng thực thi mã nguồn C#.\nNhấn "Chạy mã C#" hoặc phím tắt Ctrl + Enter để biên dịch.');
  const [isRunning, setIsRunning] = useState(false);
  const [executionStats, setExecutionStats] = useState<{ timeMs?: number; success?: boolean }>({});
  const [templates, setTemplates] = useState<CodeTemplate[]>([]);
  const [challenges, setChallenges] = useState<CodingChallenge[]>([]);
  const [activeTab, setActiveTab] = useState<'editor' | 'challenges'>('editor');
  const [selectedChallenge, setSelectedChallenge] = useState<CodingChallenge | null>(null);
  const [submissionResult, setSubmissionResult] = useState<ChallengeSubmissionResponse | null>(null);

  // Load templates & challenges on mount
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
    setExecutionStats({});

    try {
      const res = await apiClient.post<CodeExecutionResponse>('/api/playground/run', { code });
      const data = res.data;
      if (data.isSuccess) {
        setOutput(data.output || '(Chương trình thực thi thành công nhưng không có dữ liệu xuất ra Console)');
        setExecutionStats({ timeMs: data.executionTimeMs, success: true });
      } else {
        setOutput(data.error || 'Lỗi biên dịch hoặc thực thi.');
        setExecutionStats({ timeMs: data.executionTimeMs, success: false });
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Lỗi kết nối tới Backend API.';
      setOutput(`Lỗi kết nối: ${message}`);
      setExecutionStats({ success: false });
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
    setOutput(`🎯 THỬ THÁCH: ${c.title} (${c.difficulty} - +${c.xpReward} XP)\n\n${c.shortDescription}\n\nHãy hoàn thành mã nguồn và nhấn "Nộp bài chấm điểm"!`);
    setSubmissionResult(null);
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
      setSubmissionResult(data);
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
    <div className="flex flex-col h-[calc(100vh-4rem)] bg-slate-950 text-slate-100">
      {/* Top Toolbar */}
      <div className="flex flex-wrap items-center justify-between border-b border-slate-800 bg-slate-900/70 px-4 py-2.5 gap-3">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-1.5 rounded-lg bg-slate-800 p-1 border border-slate-700/60 text-xs">
            <button
              onClick={() => setActiveTab('editor')}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md font-medium transition-all ${
                activeTab === 'editor' ? 'bg-purple-600 text-white shadow' : 'text-slate-400 hover:text-white'
              }`}
            >
              <FileCode className="h-3.5 w-3.5" />
              <span>Trình soạn thảo</span>
            </button>
            <button
              onClick={() => setActiveTab('challenges')}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md font-medium transition-all ${
                activeTab === 'challenges' ? 'bg-purple-600 text-white shadow' : 'text-slate-400 hover:text-white'
              }`}
            >
              <Trophy className="h-3.5 w-3.5" />
              <span>Thử thách ({challenges.length})</span>
            </button>
          </div>

          {activeTab === 'editor' && templates.length > 0 && (
            <select
              onChange={(e) => handleSelectTemplate(e.target.value)}
              className="bg-slate-800 border border-slate-700 text-slate-300 text-xs rounded-lg px-2.5 py-1.5 focus:outline-none focus:border-purple-500"
              defaultValue=""
            >
              <option value="" disabled>-- Chọn mẫu code C# --</option>
              {templates.map(t => (
                <option key={t.id} value={t.id}>{t.title}</option>
              ))}
            </select>
          )}

          {selectedChallenge && (
            <span className="hidden sm:inline-flex items-center gap-1.5 bg-amber-500/10 border border-amber-500/30 text-amber-300 px-2.5 py-1 rounded-full text-xs font-semibold">
              <Zap className="h-3 w-3 text-amber-400" />
              {selectedChallenge.title} (+{selectedChallenge.xpReward} XP)
            </span>
          )}
        </div>

        <div className="flex items-center gap-2">
          {selectedChallenge ? (
            <button
              onClick={handleSubmitChallenge}
              disabled={isRunning}
              className="flex items-center gap-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 px-4 py-1.5 text-xs font-semibold text-white shadow-md shadow-emerald-600/20 transition-all"
            >
              <CheckCircle2 className="h-3.5 w-3.5" />
              <span>{isRunning ? 'Đang chấm...' : 'Nộp bài chấm điểm'}</span>
            </button>
          ) : (
            <button
              onClick={handleRunCode}
              disabled={isRunning}
              className="flex items-center gap-1.5 rounded-lg bg-purple-600 hover:bg-purple-500 disabled:opacity-50 px-4 py-1.5 text-xs font-semibold text-white shadow-md shadow-purple-600/20 transition-all"
            >
              <Play className="h-3.5 w-3.5 fill-current" />
              <span>{isRunning ? 'Đang chạy...' : 'Chạy mã C#'}</span>
            </button>
          )}

          <button
            onClick={() => {
              setCode(selectedChallenge ? selectedChallenge.initialCode : DEFAULT_CODE);
              setOutput('Đã hoàn tác mã nguồn.');
            }}
            title="Khôi phục mã ban đầu"
            className="p-1.5 rounded-lg border border-slate-700 bg-slate-800 hover:bg-slate-700 text-slate-300 transition-colors"
          >
            <RotateCcw className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>

      {/* Main Workspace */}
      {activeTab === 'challenges' ? (
        <div className="flex-1 overflow-y-auto p-6 max-w-5xl mx-auto w-full">
          <div className="mb-6">
            <h2 className="text-xl font-bold text-white flex items-center gap-2">
              <Trophy className="h-5 w-5 text-amber-400" />
              Danh sách Thử thách Thuật toán C#
            </h2>
            <p className="text-xs text-slate-400 mt-1">Luyện tập tư duy giải quyết bài toán và nhận thưởng XP trực tiếp</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {challenges.map((c) => (
              <div
                key={c.id}
                className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 hover:border-purple-500/40 transition-all flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-xs font-bold text-purple-400">{c.categoryName}</span>
                    <span className="text-[11px] font-semibold px-2 py-0.5 rounded bg-slate-800 text-slate-300">
                      {c.difficulty}
                    </span>
                  </div>
                  <h3 className="text-base font-bold text-white">{c.title}</h3>
                  <p className="text-xs text-slate-400 mt-2 line-clamp-2">{c.shortDescription}</p>
                </div>

                <div className="mt-5 flex items-center justify-between pt-4 border-t border-slate-800/80">
                  <span className="text-xs font-semibold text-amber-400 flex items-center gap-1">
                    <Zap className="h-3.5 w-3.5" /> +{c.xpReward} XP
                  </span>
                  <button
                    onClick={() => handleSelectChallenge(c)}
                    className="rounded-lg bg-purple-600/20 hover:bg-purple-600 hover:text-white text-purple-300 border border-purple-500/30 px-3 py-1.5 text-xs font-semibold transition-all"
                  >
                    Bắt đầu làm bài →
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      ) : (
        <div className="flex-1 flex flex-col lg:flex-row overflow-hidden">
          {/* Monaco Editor Pane */}
          <div className="flex-1 flex flex-col border-b lg:border-b-0 lg:border-r border-slate-800">
            <div className="flex items-center justify-between px-4 py-2 border-b border-slate-800/70 bg-slate-900/40 text-xs text-slate-400 font-mono">
              <span className="flex items-center gap-1.5">
                <FileCode className="h-3.5 w-3.5 text-purple-400" />
                Program.cs
              </span>
              <span>C# 14 (.NET 10)</span>
            </div>

            <div className="flex-1 relative">
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
                  padding: { top: 12 },
                  lineNumbersMinChars: 3,
                }}
              />
            </div>
          </div>

          {/* Terminal Output Pane */}
          <div className="h-64 lg:h-full lg:w-[420px] flex flex-col bg-slate-950">
            <div className="flex items-center justify-between px-4 py-2 border-b border-slate-800 bg-slate-900/80 text-xs font-medium text-slate-300">
              <span className="flex items-center gap-1.5">
                <Terminal className="h-3.5 w-3.5 text-slate-400" />
                Kết quả thực thi (Console Output)
              </span>

              {executionStats.timeMs !== undefined && (
                <span className="flex items-center gap-1 text-[11px] text-slate-400">
                  <Clock className="h-3 w-3" />
                  {executionStats.timeMs}ms
                </span>
              )}
            </div>

            <div className="flex-1 overflow-auto p-4 font-mono text-xs text-slate-200 whitespace-pre-wrap leading-relaxed select-text">
              {output}
            </div>

            {/* Status bar */}
            <div className="border-t border-slate-900 px-4 py-2 text-[11px] text-slate-500 flex items-center justify-between bg-slate-900/30">
              <span className="flex items-center gap-1.5">
                {executionStats.success === true && <CheckCircle2 className="h-3.5 w-3.5 text-emerald-400" />}
                {executionStats.success === false && <XCircle className="h-3.5 w-3.5 text-rose-400" />}
                {executionStats.success === undefined ? 'Trạng thái: Chưa thực thi' : executionStats.success ? 'Thành công (Exit 0)' : 'Có lỗi'}
              </span>
              <span>Roslyn Host: Local Sandbox</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

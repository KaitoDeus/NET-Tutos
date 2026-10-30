'use client';

import { useState, useRef, useEffect } from 'react';
import Link from 'next/link';
import apiClient from '@/lib/api';
import { AiTutorChatResponse } from '@/types/api';

interface Message {
  id: string;
  sender: 'ai' | 'user';
  text: string;
  time: string;
}

const PRESET_TOPICS = [
  { id: 'linq', title: 'Làm chủ LINQ & Deferred Exec', icon: 'bi-funnel text-primary', prompt: 'Hãy giải thích cơ chế Deferred Execution (thực thi trì hoãn) trong LINQ C#, kèm ví dụ minh họa và cạm bẫy thường gặp.' },
  { id: 'async', title: 'Bất đồng bộ Async/Await & Task', icon: 'bi-cpu text-success', prompt: 'Giải thích bản chất hoạt động của từ khóa async/await trong C#, Task vs Thread, và cách tránh Deadlock.' },
  { id: 'di', title: '3 Vòng đời Dependency Injection', icon: 'bi-diagram-3 text-info', prompt: 'So sánh chi tiết 3 vòng đời Service trong ASP.NET Core: Transient, Scoped và Singleton. Khi nào dùng loại nào?' },
  { id: 'record', title: 'Phân biệt Record, Class, Struct', icon: 'bi-box-seam text-warning', prompt: 'Phân biệt Record, Class và Struct trong C# 14. Value equality và with-expression hoạt động như thế nào?' },
  { id: 'null', title: 'Khắc phục lỗi NullReference', icon: 'bi-shield-check text-danger', prompt: 'Cách phòng tránh và xử lý triệt để lỗi NullReferenceException bằng Nullable Reference Types và Pattern Matching trong C# hiện đại.' },
];

export default function AiTutorPage() {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      sender: 'ai',
      text: 'Xin chào! Tôi là Trợ Lý AI Gia Sư .NET của bạn. Hôm nay bạn đang gặp thử thách gì với C# hoặc ASP.NET Core?\n\nBạn có thể chọn chủ đề gợi ý bên trái hoặc gõ bất kỳ câu hỏi nào bên dưới nhé!',
      time: '03:54',
    },
  ]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading]);

  const handleSendMessage = async (textToSend?: string) => {
    const text = textToSend || input;
    if (!text.trim() || isLoading) return;

    const now = new Date();
    const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

    const userMsg: Message = {
      id: Date.now().toString(),
      sender: 'user',
      text,
      time: timeStr,
    };

    setMessages((prev) => [...prev, userMsg]);
    if (!textToSend) setInput('');
    setIsLoading(true);

    try {
      const res = await apiClient.post<AiTutorChatResponse>('/api/aitutor/chat', { message: text });
      const aiMsg: Message = {
        id: (Date.now() + 1).toString(),
        sender: 'ai',
        text: res.data.responseMarkdown || 'Tôi đã tiếp nhận câu hỏi của bạn.',
        time: timeStr,
      };
      setMessages((prev) => [...prev, aiMsg]);
    } catch {
      const errorMsg: Message = {
        id: (Date.now() + 1).toString(),
        sender: 'ai',
        text: 'Xin lỗi, không thể kết nối tới Backend AI Service lúc này. Vui lòng đảm bảo backend đang hoạt động.',
        time: timeStr,
      };
      setMessages((prev) => [...prev, errorMsg]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const handleClearHistory = () => {
    setMessages([
      {
        id: '1',
        sender: 'ai',
        text: 'Đã làm mới hội thoại. Bạn có thể chọn chủ đề gợi ý bên trái hoặc gõ bất kỳ câu hỏi nào nhé!',
        time: '03:54',
      },
    ]);
  };

  return (
    <div className="container py-4">
      {/* Top Header Banner */}
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4 p-4 rounded-4 shadow-sm border bg-body">
        <div className="d-flex align-items-center gap-3">
          <div className="rounded-circle bg-primary bg-opacity-10 text-primary p-3 d-flex align-items-center justify-content-center" style={{ width: '54px', height: '54px' }}>
            <i className="bi bi-robot fs-2"></i>
          </div>
          <div>
            <div className="d-flex align-items-center gap-2 mb-1">
              <h3 className="fw-bold mb-0">Trợ Lý AI Gia Sư .NET</h3>
              <span className="badge bg-primary-subtle text-primary border border-primary-subtle rounded-pill px-2.5 py-1 fw-semibold" style={{ fontSize: '0.72rem' }}>
                <i className="bi bi-stars me-1 text-warning"></i>.NET 10 & C# 14
              </span>
            </div>
            <p className="text-muted small mb-0">
              Gia sư lập trình tương tác: Giải thích lỗi biên dịch, gợi ý thuật toán, tối ưu hóa code và giải đáp kiến thức chuyên sâu.
            </p>
          </div>
        </div>

        <div className="d-flex align-items-center gap-2">
          <Link href="/playground" className="btn btn-sm btn-outline-primary rounded-pill px-3 py-1.5 fw-semibold d-flex align-items-center gap-1.5">
            <i className="bi bi-terminal-fill"></i>
            <span>Mở C# Sandbox</span>
          </Link>
          <button type="button" className="btn btn-sm btn-outline-secondary rounded-pill px-3 py-1.5" onClick={handleClearHistory} title="Làm mới hội thoại">
            <i className="bi bi-arrow-clockwise me-1"></i>Làm mới
          </button>
        </div>
      </div>

      <div className="row g-4">
        {/* Left Sidebar: Quick Prompts & Knowledge Topics */}
        <div className="col-lg-4 col-xl-3">
          <div className="card border shadow-sm rounded-4 bg-body p-3 mb-3">
            <h6 className="fw-bold text-body-emphasis mb-3 d-flex align-items-center gap-2">
              <i className="bi bi-lightning-charge-fill text-warning"></i>
              <span>Chủ đề Thường Gặp</span>
            </h6>

            <div className="d-flex flex-column gap-2">
              {PRESET_TOPICS.map((topic) => (
                <button
                  key={topic.id}
                  type="button"
                  className="preset-topic-btn btn btn-sm rounded-3 p-2.5 d-flex align-items-center gap-2 w-100"
                  onClick={() => handleSendMessage(topic.prompt)}
                  disabled={isLoading}
                >
                  <i className={`bi ${topic.icon} fs-6`}></i>
                  <span className="small fw-semibold">{topic.title}</span>
                </button>
              ))}
            </div>
          </div>

          {/* Features Info Box */}
          <div className="card border-0 rounded-4 p-3 bg-primary-subtle text-primary">
            <div className="d-flex align-items-center gap-2 mb-2">
              <i className="bi bi-info-circle-fill"></i>
              <span className="fw-bold small">Hỗ trợ Đa năng</span>
            </div>
            <p className="small mb-0 opacity-90 lh-base" style={{ fontSize: '0.8rem' }}>
              Bạn có thể dán mã lỗi biên dịch (vd: CS0103, CS0029) hoặc yêu cầu review code trực tiếp trong khung chat.
            </p>
          </div>
        </div>

        {/* Main Chat Area */}
        <div className="col-lg-8 col-xl-9">
          <div className="card border shadow-sm rounded-4 bg-body d-flex flex-column" style={{ height: '650px' }}>
            {/* Chat Messages Scroll Container */}
            <div className="flex-grow-1 p-3 p-md-4 overflow-y-auto" id="chatMessagesContainer">
              {messages.map((m) => (
                <div key={m.id} className={`d-flex gap-3 mb-4 ${m.sender === 'user' ? 'flex-row-reverse' : 'ai-msg-item'}`}>
                  <div
                    className={`rounded-circle d-flex align-items-center justify-content-center flex-shrink-0 ${
                      m.sender === 'user' ? 'bg-primary text-white' : 'bg-primary text-white'
                    }`}
                    style={{ width: '38px', height: '38px' }}
                  >
                    <i className={`bi ${m.sender === 'user' ? 'bi-person-fill' : 'bi-robot'} fs-5`}></i>
                  </div>

                  <div className="flex-grow-1" style={{ maxWidth: '85%' }}>
                    <div className={`p-3 rounded-4 ${m.sender === 'user' ? 'bg-primary text-white shadow-sm' : 'bg-body-tertiary border text-body shadow-xs'}`}>
                      {m.sender === 'ai' && (
                        <div className="fw-bold mb-1 text-primary small d-flex align-items-center gap-2">
                          <span>NET-Tutos AI Mentor</span>
                          <span className="badge bg-success-subtle text-success" style={{ fontSize: '0.65rem' }}>Online</span>
                        </div>
                      )}
                      <div className="message-content leading-relaxed whitespace-pre-wrap" style={{ fontSize: '0.92rem' }}>
                        {m.text}
                      </div>
                      <div className={`small mt-2 ${m.sender === 'user' ? 'text-white-50 text-end' : 'text-body-secondary'}`} style={{ fontSize: '0.72rem' }}>
                        {m.time}
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              {isLoading && (
                <div className="d-flex gap-3 mb-4 ai-msg-item">
                  <div className="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center flex-shrink-0" style={{ width: '38px', height: '38px' }}>
                    <i className="bi bi-robot fs-5"></i>
                  </div>
                  <div className="p-3 rounded-4 bg-body-tertiary border text-body shadow-xs d-flex align-items-center gap-2">
                    <span className="spinner-border spinner-border-sm text-primary" role="status"></span>
                    <span className="small text-muted">AI Mentor đang phân tích và soạn câu trả lời...</span>
                  </div>
                </div>
              )}

              <div ref={messagesEndRef} />
            </div>

            {/* Chat Input Container */}
            <div className="p-3 border-top bg-body-tertiary rounded-bottom-4">
              <div className="input-group">
                <textarea
                  className="form-control border-end-0 rounded-start-3 shadow-none bg-body"
                  placeholder="Hỏi về C#/.NET hoặc dán mã lỗi biên dịch... (Enter để gửi)"
                  rows={2}
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  disabled={isLoading}
                  style={{ resize: 'none' }}
                />
                <button
                  type="button"
                  className="btn btn-primary rounded-end-3 px-3.5 d-flex align-items-center justify-content-center"
                  onClick={() => handleSendMessage()}
                  disabled={isLoading || !input.trim()}
                >
                  <i className="bi bi-send-fill fs-5"></i>
                </button>
              </div>

              <div className="d-flex align-items-center justify-content-between mt-2 px-1 text-muted small" style={{ fontSize: '0.75rem' }}>
                <span>
                  <i className="bi bi-keyboard me-1"></i>Nhấn Enter để gửi, Shift+Enter để xuống dòng
                </span>
                <span className="d-none d-sm-inline">C# 14 • ASP.NET Core 10</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

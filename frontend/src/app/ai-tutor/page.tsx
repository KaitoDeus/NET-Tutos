'use client';

import { useState } from 'react';
import { Bot, Send, Sparkles, Terminal, ShieldAlert, Cpu, Layers, HelpCircle, User } from 'lucide-react';
import apiClient from '@/lib/api';
import { AiTutorChatResponse } from '@/types/api';

interface Message {
  id: string;
  sender: 'mentor' | 'user';
  text: string;
  time: string;
}

const PRESET_TOPICS = [
  { id: 'linq', title: 'Làm chủ LINQ & Deferred Exec', icon: Cpu, prompt: 'Hãy giải thích cơ chế Deferred Execution (thực thi trì hoãn) trong LINQ C#, kèm ví dụ minh họa và cạm bẫy thường gặp.' },
  { id: 'async', title: 'Bất đồng bộ Async/Await & Task', icon: Sparkles, prompt: 'Giải thích bản chất hoạt động của từ khóa async/await trong C#, Task vs Thread, và cách tránh Deadlock.' },
  { id: 'di', title: '3 Vòng đời Dependency Injection', icon: Layers, prompt: 'So sánh chi tiết 3 vòng đời Service trong ASP.NET Core: Transient, Scoped và Singleton. Khi nào dùng loại nào?' },
  { id: 'record', title: 'Phân biệt Record, Class, Struct', icon: HelpCircle, prompt: 'Phân biệt Record, Class và Struct trong C# 14. Value equality và with-expression hoạt động như thế nào?' },
  { id: 'null', title: 'Khắc phục lỗi NullReference', icon: ShieldAlert, prompt: 'Cách phòng tránh và xử lý triệt để lỗi NullReferenceException bằng Nullable Reference Types và Pattern Matching trong C# hiện đại.' },
];

export default function AiTutorPage() {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      sender: 'mentor',
      text: 'Xin chào! Tôi là Trợ Lý AI Gia Sư .NET của bạn. Hôm nay bạn đang gặp thử thách gì với C# hoặc ASP.NET Core?\n\nBạn có thể chọn chủ đề gợi ý bên trái hoặc gõ bất kỳ câu hỏi/dán mã lỗi biên dịch vào bên dưới nhé!',
      time: '04:00',
    },
  ]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSendMessage = async (textToSend?: string) => {
    const text = textToSend || input;
    if (!text.trim() || isLoading) return;

    const userMsg: Message = {
      id: Date.now().toString(),
      sender: 'user',
      text,
      time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    };

    setMessages((prev) => [...prev, userMsg]);
    if (!textToSend) setInput('');
    setIsLoading(true);

    try {
      const res = await apiClient.post<AiTutorChatResponse>('/api/aitutor/chat', { message: text });
      const mentorMsg: Message = {
        id: (Date.now() + 1).toString(),
        sender: 'mentor',
        text: res.data.responseMarkdown || 'Tôi đã tiếp nhận câu hỏi của bạn.',
        time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      };
      setMessages((prev) => [...prev, mentorMsg]);
    } catch {
      const errorMsg: Message = {
        id: (Date.now() + 1).toString(),
        sender: 'mentor',
        text: 'Xin lỗi, không thể kết nối tới Backend AI Service lúc này. Vui lòng đảm bảo backend đang hoạt động.',
        time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      };
      setMessages((prev) => [...prev, errorMsg]);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8 flex flex-col gap-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-800 pb-5">
        <div className="flex items-center gap-4">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-purple-600 to-indigo-600 shadow-lg shadow-purple-600/30">
            <Bot className="h-6 w-6 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold text-white">Trợ Lý AI Gia Sư .NET</h1>
              <span className="rounded-full bg-purple-500/10 px-2.5 py-0.5 text-xs font-semibold text-purple-300 border border-purple-500/20">
                .NET 10 & C# 14
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-0.5">
              Giải thích lỗi biên dịch, gợi ý thuật toán, tối ưu hóa code và giải đáp kiến thức chuyên sâu.
            </p>
          </div>
        </div>

        <a
          href="/playground"
          className="flex items-center gap-2 rounded-xl border border-slate-700 bg-slate-900 px-4 py-2 text-xs font-semibold text-purple-300 hover:bg-slate-800 hover:text-white transition-colors"
        >
          <Terminal className="h-3.5 w-3.5" />
          <span>Mở C# Sandbox</span>
        </a>
      </div>

      {/* Main Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6 min-h-[580px]">
        {/* Left Sidebar: Presets */}
        <div className="flex flex-col gap-4">
          <div className="rounded-2xl border border-slate-800 bg-slate-900/60 p-5">
            <h2 className="text-xs font-bold text-slate-300 uppercase tracking-wider mb-4 flex items-center gap-2">
              <Sparkles className="h-3.5 w-3.5 text-amber-400" />
              Chủ đề Thường Gặp
            </h2>

            <div className="flex flex-col gap-2.5">
              {PRESET_TOPICS.map((topic) => {
                const Icon = topic.icon;
                return (
                  <button
                    key={topic.id}
                    onClick={() => handleSendMessage(topic.prompt)}
                    disabled={isLoading}
                    className="flex items-center gap-2.5 w-full text-left rounded-xl border border-slate-800 bg-slate-950/80 hover:bg-slate-800 hover:border-purple-500/50 p-3 text-xs font-medium text-slate-200 transition-all hover:translate-x-1 disabled:opacity-50 group"
                  >
                    <Icon className="h-4 w-4 text-purple-400 flex-shrink-0 group-hover:scale-110 transition-transform" />
                    <span className="line-clamp-1">{topic.title}</span>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="rounded-2xl border border-blue-900/40 bg-blue-950/20 p-4 text-xs text-blue-300 leading-relaxed">
            <p className="font-semibold text-blue-200 mb-1">💡 Hỗ trợ Đa năng</p>
            Bạn có thể dán trực tiếp mã lỗi biên dịch (vd: CS0103, CS0029) hoặc yêu cầu review mã nguồn C# ngay trong khung chat.
          </div>
        </div>

        {/* Right Chat Area */}
        <div className="lg:col-span-3 flex flex-col rounded-2xl border border-slate-800 bg-slate-900/40 overflow-hidden shadow-xl">
          {/* Messages Feed */}
          <div className="flex-1 overflow-y-auto p-6 flex flex-col gap-5 max-h-[500px]">
            {messages.map((m) => (
              <div
                key={m.id}
                className={`flex gap-3.5 max-w-2xl ${
                  m.sender === 'user' ? 'ml-auto flex-row-reverse' : ''
                }`}
              >
                <div
                  className={`flex h-8 w-8 items-center justify-center rounded-xl flex-shrink-0 ${
                    m.sender === 'user'
                      ? 'bg-purple-600 text-white'
                      : 'bg-indigo-600 text-white'
                  }`}
                >
                  {m.sender === 'user' ? <User className="h-4 w-4" /> : <Bot className="h-4 w-4" />}
                </div>

                <div
                  className={`rounded-2xl px-5 py-4 text-sm leading-relaxed ${
                    m.sender === 'user'
                      ? 'bg-purple-600 text-white rounded-tr-none'
                      : 'bg-slate-950 border border-slate-800 text-slate-200 rounded-tl-none whitespace-pre-wrap'
                  }`}
                >
                  {m.sender === 'mentor' && (
                    <div className="flex items-center gap-2 mb-2 pb-1 border-b border-slate-800/80">
                      <span className="text-xs font-bold text-purple-400">NET-Tutos AI Mentor</span>
                      <span className="inline-flex items-center gap-1 rounded-full bg-emerald-500/10 px-2 py-0.5 text-[10px] font-semibold text-emerald-400 border border-emerald-500/20">
                        Online
                      </span>
                    </div>
                  )}
                  {m.text}
                  <div className="mt-2 text-[10px] text-slate-400 text-right">{m.time}</div>
                </div>
              </div>
            ))}

            {isLoading && (
              <div className="flex items-center gap-2 text-xs text-purple-400 bg-purple-500/10 border border-purple-500/20 rounded-xl px-4 py-3 w-fit">
                <Sparkles className="h-3.5 w-3.5 animate-spin" />
                <span>AI Mentor đang suy nghĩ và phân tích...</span>
              </div>
            )}
          </div>

          {/* Input Box */}
          <div className="border-t border-slate-800 bg-slate-950/80 p-4">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                handleSendMessage();
              }}
              className="flex items-center gap-3"
            >
              <input
                type="text"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                placeholder="Hỏi về C#/.NET hoặc dán mã lỗi biên dịch... (Enter để gửi)"
                disabled={isLoading}
                className="flex-1 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-white placeholder-slate-400 focus:outline-none focus:border-purple-500 disabled:opacity-50"
              />
              <button
                type="submit"
                disabled={isLoading || !input.trim()}
                className="flex h-11 w-11 items-center justify-center rounded-xl bg-purple-600 hover:bg-purple-500 text-white disabled:opacity-40 transition-colors shadow-md shadow-purple-600/30"
              >
                <Send className="h-4 w-4" />
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}

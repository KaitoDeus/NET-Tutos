import * as crypto from 'node:crypto';
import * as path from 'node:path';
import * as vscode from 'vscode';
import { AtusClient, AtusLanguage, IdeKnowledgeHit } from './atusClient';

export interface ChatCardMessage {
  role: 'user' | 'assistant' | 'system';
  title?: string;
  markdown: string;
  codeSnippet?: string | null;
  references?: IdeKnowledgeHit[];
  meta?: string;
}

export class AtusChatViewProvider implements vscode.WebviewViewProvider {
  public static readonly viewType = 'atus.chatView';

  private view?: vscode.WebviewView;
  private readonly history: ChatCardMessage[] = [];

  constructor(
    private readonly extensionUri: vscode.Uri,
    private readonly client: AtusClient,
    private readonly onLanguageChanged: (lang: AtusLanguage) => Promise<void>
  ) {}

  public resolveWebviewView(webviewView: vscode.WebviewView): void {
    this.view = webviewView;
    webviewView.webview.options = {
      enableScripts: true,
      localResourceRoots: [this.extensionUri]
    };

    webviewView.webview.html = this.renderHtml(webviewView.webview);

    webviewView.webview.onDidReceiveMessage(async (msg) => {
      if (!msg || typeof msg !== 'object') return;

      switch (msg.type) {
        case 'ready':
          this.postState();
          break;
        case 'ask':
          if (typeof msg.prompt === 'string' && msg.prompt.trim().length > 0) {
            await this.handleChatPrompt(msg.prompt.trim(), Boolean(msg.attachActiveCode));
          }
          break;
        case 'quickCommand':
          if (typeof msg.command === 'string') {
            await vscode.commands.executeCommand(msg.command);
          }
          break;
        case 'setLanguage':
          if (msg.lang === 'vi' || msg.lang === 'en') {
            await this.onLanguageChanged(msg.lang);
            this.postState();
          }
          break;
        case 'insertCode':
          if (typeof msg.code === 'string') {
            await this.insertCodeIntoActiveEditor(msg.code);
          }
          break;
        case 'openUrl':
          if (typeof msg.url === 'string' && msg.url.startsWith('/')) {
            const fullUrl = `${this.client.getBaseUrl()}${msg.url}`;
            await vscode.env.openExternal(vscode.Uri.parse(fullUrl));
          }
          break;
        case 'clear':
          this.history.length = 0;
          this.postState();
          break;
      }
    });
  }

  public async focus(): Promise<void> {
    if (this.view) {
      this.view.show?.(true);
    } else {
      await vscode.commands.executeCommand('atus.chatView.focus');
    }
  }

  public pushAssistantMessage(message: ChatCardMessage): void {
    this.history.push(message);
    if (this.history.length > 40) {
      this.history.shift();
    }
    this.postState();
  }

  public notifyConfigChanged(): void {
    this.postState();
  }

  private async handleChatPrompt(prompt: string, attachActiveCode: boolean): Promise<void> {
    const editor = vscode.window.activeTextEditor;
    let codeContext: string | undefined;
    let fileName: string | undefined;
    let errorContext: string | undefined;

    if (attachActiveCode && editor) {
      fileName = path.basename(editor.document.fileName || 'Program.cs');
      const selection = editor.selection;
      const rawCode = !selection.isEmpty ? editor.document.getText(selection) : editor.document.getText();
      codeContext = rawCode.length > 6000 ? rawCode.slice(0, 6000) : rawCode;

      const diags = vscode.languages
        .getDiagnostics(editor.document.uri)
        .filter((d) => d.severity === vscode.DiagnosticSeverity.Error || d.severity === vscode.DiagnosticSeverity.Warning)
        .slice(0, 5)
        .map((d) => `[Line ${d.range.start.line + 1}] ${d.message}`);

      if (diags.length > 0) {
        errorContext = diags.join('\n');
      }
    }

    this.pushAssistantMessage({
      role: 'user',
      markdown: prompt,
      meta: fileName ? `Context: ${fileName}` : undefined
    });

    this.view?.webview.postMessage({ type: 'busy', busy: true });
    try {
      const reply = await this.client.chat({
        message: prompt,
        codeContext,
        fileName,
        errorContext
      });

      this.pushAssistantMessage({
        role: 'assistant',
        title: `Atus (${reply.modelUsed || 'AI'})`,
        markdown: reply.responseMarkdown,
        codeSnippet: reply.codeSnippet,
        references: reply.references
      });
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      this.pushAssistantMessage({
        role: 'system',
        title: 'Connection / Request Notice',
        markdown: `Unable to reach **${this.client.getBaseUrl()}**: \`${msg}\`\n\nVerify NET-Tutos Docker/Web is running or update \`atus.serverUrl\` in Settings.`
      });
    } finally {
      this.view?.webview.postMessage({ type: 'busy', busy: false });
    }
  }

  private async insertCodeIntoActiveEditor(code: string): Promise<void> {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
      const doc = await vscode.workspace.openTextDocument({ language: 'csharp', content: code });
      await vscode.window.showTextDocument(doc, vscode.ViewColumn.Beside);
      return;
    }

    await editor.edit((editBuilder) => {
      if (!editor.selection.isEmpty) {
        editBuilder.replace(editor.selection, code);
      } else {
        editBuilder.insert(editor.selection.active, code);
      }
    });
  }

  private postState(): void {
    this.view?.webview.postMessage({
      type: 'state',
      language: this.client.getLanguage(),
      serverUrl: this.client.getBaseUrl(),
      authenticated: this.client.isAuthenticated(),
      messages: this.history
    });
  }

  private renderHtml(webview: vscode.Webview): string {
    const nonce = crypto.randomBytes(16).toString('base64');
    return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src ${webview.cspSource} 'nonce-${nonce}'; script-src 'nonce-${nonce}';" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<style nonce="${nonce}">
  * { box-sizing: border-box; }
  body {
    margin: 0;
    padding: 10px;
    font-family: var(--vscode-font-family);
    font-size: var(--vscode-font-size);
    color: var(--vscode-foreground);
    background: var(--vscode-sideBar-background);
    display: flex;
    flex-direction: column;
    height: 100vh;
    gap: 8px;
  }
  .header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 6px;
    border-bottom: 1px solid var(--vscode-panel-border);
    padding-bottom: 6px;
  }
  .brand {
    font-weight: 700;
    font-size: 12px;
    display: flex;
    align-items: center;
    gap: 6px;
  }
  .badge {
    font-size: 10px;
    padding: 2px 6px;
    border-radius: 999px;
    background: var(--vscode-badge-background);
    color: var(--vscode-badge-foreground);
  }
  .controls {
    display: flex;
    gap: 4px;
    align-items: center;
  }
  select, button {
    font-family: inherit;
    font-size: 11px;
    border-radius: 4px;
    cursor: pointer;
  }
  select {
    background: var(--vscode-dropdown-background);
    color: var(--vscode-dropdown-foreground);
    border: 1px solid var(--vscode-dropdown-border);
    padding: 2px 5px;
  }
  .btn-secondary {
    background: var(--vscode-button-secondaryBackground);
    color: var(--vscode-button-secondaryForeground);
    border: none;
    padding: 4px 8px;
  }
  .btn-secondary:hover { background: var(--vscode-button-secondaryHoverBackground); }
  .btn-primary {
    background: var(--vscode-button-background);
    color: var(--vscode-button-foreground);
    border: none;
    padding: 6px 10px;
    font-weight: 600;
  }
  .btn-primary:hover { background: var(--vscode-button-hoverBackground); }
  .btn-primary:disabled { opacity: 0.5; cursor: wait; }
  .quick-bar {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
  }
  .chip {
    background: var(--vscode-button-secondaryBackground);
    color: var(--vscode-button-secondaryForeground);
    border: 1px solid var(--vscode-panel-border);
    padding: 3px 7px;
    font-size: 11px;
    border-radius: 999px;
  }
  .chip:hover { background: var(--vscode-button-secondaryHoverBackground); }
  #messages {
    flex: 1;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding-right: 2px;
  }
  .card {
    border: 1px solid var(--vscode-panel-border);
    border-radius: 6px;
    padding: 8px 10px;
    background: var(--vscode-editor-background);
  }
  .card.user {
    border-left: 3px solid var(--vscode-textLink-foreground);
  }
  .card.assistant {
    border-left: 3px solid var(--vscode-charts-purple, #8b5cf6);
  }
  .card.system {
    border-left: 3px solid var(--vscode-editorWarning-foreground);
  }
  .card-title {
    font-size: 11px;
    font-weight: 700;
    opacity: 0.85;
    margin-bottom: 4px;
    display: flex;
    justify-content: space-between;
  }
  .card-body {
    line-height: 1.45;
    word-break: break-word;
    white-space: pre-wrap;
  }
  pre {
    background: var(--vscode-textBlockQuote-background);
    border: 1px solid var(--vscode-panel-border);
    border-radius: 4px;
    padding: 7px;
    overflow-x: auto;
    font-family: var(--vscode-editor-font-family);
    font-size: 11px;
    margin: 6px 0 4px 0;
  }
  .code-actions {
    display: flex;
    gap: 6px;
    margin-bottom: 4px;
  }
  .refs {
    margin-top: 6px;
    border-top: 1px dashed var(--vscode-panel-border);
    padding-top: 5px;
    display: flex;
    flex-direction: column;
    gap: 3px;
  }
  .ref-link {
    text-align: left;
    background: transparent;
    border: none;
    padding: 2px 0;
    color: var(--vscode-textLink-foreground);
    font-size: 11px;
  }
  .ref-link:hover { text-decoration: underline; }
  .composer {
    border-top: 1px solid var(--vscode-panel-border);
    padding-top: 8px;
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  textarea {
    width: 100%;
    min-height: 58px;
    resize: vertical;
    background: var(--vscode-input-background);
    color: var(--vscode-input-foreground);
    border: 1px solid var(--vscode-input-border);
    border-radius: 4px;
    padding: 6px 8px;
    font-family: inherit;
    font-size: 12px;
  }
  .composer-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 6px;
  }
  label.ctx {
    font-size: 11px;
    display: flex;
    align-items: center;
    gap: 4px;
    opacity: 0.9;
  }
  .empty {
    opacity: 0.75;
    font-size: 12px;
    padding: 12px 6px;
    text-align: center;
    line-height: 1.5;
  }
</style>
</head>
<body>
  <div class="header">
    <div class="brand">
      <span>Atus AI</span>
      <span id="authBadge" class="badge">Guest</span>
    </div>
    <div class="controls">
      <select id="langSelect" title="Response Language">
        <option value="vi">VI</option>
        <option value="en">EN</option>
      </select>
      <button id="clearBtn" class="btn-secondary" title="Clear chat">Clear</button>
    </div>
  </div>

  <div class="quick-bar">
    <button class="chip" data-cmd="atus.reviewCurrentFile">Review &amp; Big-O</button>
    <button class="chip" data-cmd="atus.explainSelectionOrDiagnostics">Explain Error</button>
    <button class="chip" data-cmd="atus.generateUnitTests">xUnit Tests</button>
    <button class="chip" data-cmd="atus.runInSandbox">Run Roslyn</button>
    <button class="chip" data-cmd="atus.searchKnowledge">RAG Search</button>
  </div>

  <div id="messages"></div>

  <div class="composer">
    <textarea id="promptInput" placeholder="Ask Atus about C#, .NET 10, EF Core, async/await, or your active file... (Ctrl+Enter to send)"></textarea>
    <div class="composer-footer">
      <label class="ctx"><input id="attachCode" type="checkbox" checked /> Attach active C# context</label>
      <button id="sendBtn" class="btn-primary">Ask Atus</button>
    </div>
  </div>

<script nonce="${nonce}">
  const vscode = acquireVsCodeApi();
  const messagesEl = document.getElementById('messages');
  const promptEl = document.getElementById('promptInput');
  const sendBtn = document.getElementById('sendBtn');
  const attachEl = document.getElementById('attachCode');
  const langSelect = document.getElementById('langSelect');
  const authBadge = document.getElementById('authBadge');
  const clearBtn = document.getElementById('clearBtn');

  let state = { language: 'vi', serverUrl: '', authenticated: false, messages: [] };

  function escapeText(str) {
    return String(str ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
  }

  function formatInlineMarkdown(text) {
    return escapeText(text)
      .replace(/\\*\\*([^*]+)\\*\\*/g, '<strong>$1</strong>')
      .replace(/\`([^\`]+)\`/g, '<code>$1</code>');
  }

  function renderMessages() {
    authBadge.textContent = state.authenticated ? 'Signed In' : 'Guest';
    langSelect.value = state.language === 'en' ? 'en' : 'vi';

    if (!state.messages || state.messages.length === 0) {
      const welcome = state.language === 'en'
        ? 'Open any C# file and click a quick action above, or ask Atus about .NET 10 architecture, LINQ, EF Core, or Kafka.'
        : 'Mở file C# bất kỳ rồi nhấn nút thao tác nhanh phía trên, hoặc hỏi Atus về kiến trúc .NET 10, LINQ, EF Core, Kafka.';
      messagesEl.innerHTML = '<div class="empty">' + escapeText(welcome) + '</div>';
      return;
    }

    messagesEl.innerHTML = '';
    for (const msg of state.messages) {
      const card = document.createElement('div');
      card.className = 'card ' + (msg.role || 'assistant');

      const title = document.createElement('div');
      title.className = 'card-title';
      title.innerHTML = '<span>' + escapeText(msg.title || (msg.role === 'user' ? 'You' : 'Atus')) + '</span>'
        + (msg.meta ? '<span>' + escapeText(msg.meta) + '</span>' : '');
      card.appendChild(title);

      const body = document.createElement('div');
      body.className = 'card-body';
      body.innerHTML = formatInlineMarkdown(msg.markdown || '');
      card.appendChild(body);

      if (msg.codeSnippet) {
        const pre = document.createElement('pre');
        pre.textContent = msg.codeSnippet;
        card.appendChild(pre);

        const act = document.createElement('div');
        act.className = 'code-actions';
        const insertBtn = document.createElement('button');
        insertBtn.className = 'btn-secondary';
        insertBtn.textContent = 'Insert / Apply Code';
        insertBtn.addEventListener('click', () => {
          vscode.postMessage({ type: 'insertCode', code: msg.codeSnippet });
        });
        act.appendChild(insertBtn);
        card.appendChild(act);
      }

      if (Array.isArray(msg.references) && msg.references.length > 0) {
        const refsDiv = document.createElement('div');
        refsDiv.className = 'refs';
        for (const hit of msg.references) {
          const btn = document.createElement('button');
          btn.className = 'ref-link';
          btn.textContent = '[' + (hit.sourceType || 'Docs') + '] ' + (hit.title || hit.url);
          btn.addEventListener('click', () => {
            vscode.postMessage({ type: 'openUrl', url: hit.url });
          });
          refsDiv.appendChild(btn);
        }
        card.appendChild(refsDiv);
      }

      messagesEl.appendChild(card);
    }
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function submitPrompt() {
    const prompt = promptEl.value.trim();
    if (!prompt) return;
    promptEl.value = '';
    vscode.postMessage({
      type: 'ask',
      prompt,
      attachActiveCode: attachEl.checked
    });
  }

  sendBtn.addEventListener('click', submitPrompt);
  promptEl.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      submitPrompt();
    }
  });

  langSelect.addEventListener('change', () => {
    vscode.postMessage({ type: 'setLanguage', lang: langSelect.value });
  });

  clearBtn.addEventListener('click', () => {
    vscode.postMessage({ type: 'clear' });
  });

  document.querySelectorAll('.chip[data-cmd]').forEach((btn) => {
    btn.addEventListener('click', () => {
      vscode.postMessage({ type: 'quickCommand', command: btn.getAttribute('data-cmd') });
    });
  });

  window.addEventListener('message', (event) => {
    const data = event.data;
    if (!data) return;
    if (data.type === 'state') {
      state = data;
      renderMessages();
    } else if (data.type === 'busy') {
      sendBtn.disabled = Boolean(data.busy);
      sendBtn.textContent = data.busy ? 'Thinking...' : 'Ask Atus';
    }
  });

  vscode.postMessage({ type: 'ready' });
</script>
</body>
</html>`;
  }
}

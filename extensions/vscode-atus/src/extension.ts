import * as path from 'node:path';
import * as vscode from 'vscode';
import { AtusCodeActionProvider } from './atusCodeActionProvider';
import { AtusClient, AtusLanguage } from './atusClient';
import { AtusChatViewProvider } from './chatViewProvider';
import { formatReviewMarkdown, mapReviewToDiagnostics } from './reviewMapper';

const AUTH_SECRET_KEY = 'atus.jwtToken';

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const outputChannel = vscode.window.createOutputChannel('Atus Roslyn Sandbox');
  const diagnostics = vscode.languages.createDiagnosticCollection('atus-review');
  context.subscriptions.push(outputChannel, diagnostics);

  const client = await createConfiguredClient(context.secrets);

  const chatProvider = new AtusChatViewProvider(context.extensionUri, client, async (lang) => {
    await vscode.workspace.getConfiguration('atus').update('language', lang, vscode.ConfigurationTarget.Global);
    client.updateConfig({ language: lang });
  });

  context.subscriptions.push(
    vscode.window.registerWebviewViewProvider(AtusChatViewProvider.viewType, chatProvider, {
      webviewOptions: { retainContextWhenHidden: true }
    })
  );

  context.subscriptions.push(
    vscode.languages.registerCodeActionsProvider(
      [{ language: 'csharp', scheme: 'file' }, { language: 'csharp', scheme: 'untitled' }],
      new AtusCodeActionProvider(),
      { providedCodeActionKinds: AtusCodeActionProvider.providedCodeActionKinds }
    )
  );

  const statusBar = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
  statusBar.command = 'atus.openChat';
  statusBar.text = '$(sparkle) Atus AI';
  statusBar.tooltip = `NET-Tutos Pair Programmer (${client.getBaseUrl()})`;
  statusBar.show();
  context.subscriptions.push(statusBar);

  let lastRefactorState: { uri: vscode.Uri; code: string } | null = null;

  context.subscriptions.push(
    vscode.workspace.onDidChangeConfiguration(async (e) => {
      if (e.affectsConfiguration('atus')) {
        const refreshed = await readSettings();
        client.updateConfig(refreshed);
        statusBar.tooltip = `NET-Tutos Pair Programmer (${client.getBaseUrl()})`;
        chatProvider.notifyConfigChanged();
      }
    })
  );

  context.subscriptions.push(
    vscode.workspace.onDidSaveTextDocument(async (doc) => {
      const autoReview = vscode.workspace.getConfiguration('atus').get<boolean>('autoReviewOnSave', false);
      if (autoReview && doc.languageId === 'csharp' && !doc.isUntitled) {
        await runReviewForDocument(doc, doc.getText(), false);
      }
    })
  );

  async function runReviewForDocument(
    doc: vscode.TextDocument,
    sourceCode: string,
    openChatAfter = true
  ): Promise<void> {
    await vscode.window.withProgress(
      { location: vscode.ProgressLocation.Notification, title: 'Atus is reviewing C# code & Big-O...', cancellable: false },
      async () => {
        try {
          const bundle = await client.reviewCode({
            sourceCode,
            fileName: path.basename(doc.fileName || 'Program.cs')
          });

          const mapped = mapReviewToDiagnostics(sourceCode, bundle);
          const vsDiags = mapped.map((m) => {
            const safeLine = Math.min(Math.max(0, m.line), Math.max(0, doc.lineCount - 1));
            const lineRange = doc.lineAt(safeLine).range;
            const sev =
              m.severity === 'error'
                ? vscode.DiagnosticSeverity.Error
                : m.severity === 'warning'
                  ? vscode.DiagnosticSeverity.Warning
                  : vscode.DiagnosticSeverity.Information;
            const d = new vscode.Diagnostic(lineRange, m.message, sev);
            d.source = m.source;
            return d;
          });
          diagnostics.set(doc.uri, vsDiags);

          if (bundle.review.refactoredCode && bundle.review.refactoredCode.trim().length > 0) {
            lastRefactorState = { uri: doc.uri, code: bundle.review.refactoredCode };
          }

          if (openChatAfter) {
            await chatProvider.focus();
            chatProvider.pushAssistantMessage({
              role: 'assistant',
              title: `Code Review (${bundle.review.score}/100 | ${bundle.complexity.timeComplexity})`,
              markdown: formatReviewMarkdown(bundle, client.getLanguage()),
              codeSnippet: bundle.review.refactoredCode,
              meta: `${vsDiags.length} inline findings`
            });
          }
        } catch (err) {
          vscode.window.showErrorMessage(`Atus Review failed: ${getErrorMessage(err)}`);
        }
      }
    );
  }

  context.subscriptions.push(
    vscode.commands.registerCommand('atus.openChat', async () => {
      await chatProvider.focus();
    }),

    vscode.commands.registerCommand('atus.reviewCurrentFile', async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) {
        vscode.window.showInformationMessage('Open a C# file to run Atus Code Review.');
        return;
      }
      const code = !editor.selection.isEmpty ? editor.document.getText(editor.selection) : editor.document.getText();
      await runReviewForDocument(editor.document, code, true);
    }),

    vscode.commands.registerCommand(
      'atus.explainSelectionOrDiagnostics',
      async (providedError?: string, providedRange?: vscode.Range) => {
        const editor = vscode.window.activeTextEditor;
        if (!editor) {
          vscode.window.showInformationMessage('Open a file with code or compiler diagnostics first.');
          return;
        }

        const diags = vscode.languages.getDiagnostics(editor.document.uri);
        const errorText =
          typeof providedError === 'string' && providedError.trim().length > 0
            ? providedError
            : diags.length > 0
              ? diags.slice(0, 5).map((d) => `[Line ${d.range.start.line + 1}] ${d.message}`).join('\n')
              : !editor.selection.isEmpty
                ? `Explain this C# code:\n${editor.document.getText(editor.selection)}`
                : '';

        if (!errorText) {
          vscode.window.showInformationMessage('Select code or place cursor on a diagnostic to explain.');
          return;
        }

        const codeScope = providedRange && !providedRange.isEmpty
          ? editor.document.getText(providedRange)
          : !editor.selection.isEmpty
            ? editor.document.getText(editor.selection)
            : editor.document.getText();

        await vscode.window.withProgress(
          { location: vscode.ProgressLocation.Notification, title: 'Atus is diagnosing error...', cancellable: false },
          async () => {
            try {
              const res = await client.explainError({
                errorMessage: errorText,
                sourceCode: codeScope.slice(0, 6000),
                fileName: path.basename(editor.document.fileName || 'Program.cs')
              });
              await chatProvider.focus();
              chatProvider.pushAssistantMessage({
                role: 'assistant',
                title: `Error Diagnosis${res.errorCode ? ` (${res.errorCode})` : ''}`,
                markdown: `**${res.errorTitle}**\n\n${res.explanation}\n\n**Fix:** ${res.howToFix}${res.proTip ? `\n\n> Pro Tip: ${res.proTip}` : ''}`,
                codeSnippet: res.suggestedFixCode
              });
            } catch (err) {
              vscode.window.showErrorMessage(`Atus Explain Error failed: ${getErrorMessage(err)}`);
            }
          }
        );
      }
    ),

    vscode.commands.registerCommand('atus.generateUnitTests', async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) {
        vscode.window.showInformationMessage('Open a C# file to generate xUnit tests.');
        return;
      }

      const code = !editor.selection.isEmpty ? editor.document.getText(editor.selection) : editor.document.getText();
      const fileName = path.basename(editor.document.fileName || 'Target.cs');

      await vscode.window.withProgress(
        { location: vscode.ProgressLocation.Notification, title: 'Atus is generating xUnit tests...', cancellable: false },
        async () => {
          try {
            const res = await client.generateUnitTests({ sourceCode: code, fileName });
            const testDoc = await vscode.workspace.openTextDocument({
              language: 'csharp',
              content: res.testCode || '// No test code generated.'
            });
            await vscode.window.showTextDocument(testDoc, vscode.ViewColumn.Beside);
            chatProvider.pushAssistantMessage({
              role: 'assistant',
              title: `xUnit Generator (${res.testFramework})`,
              markdown: `${res.explanation}\n\n**Scenarios:**\n${(res.coveredScenarios || []).map((s) => `- ${s}`).join('\n')}`,
              codeSnippet: res.testCode
            });
          } catch (err) {
            vscode.window.showErrorMessage(`Atus xUnit Generator failed: ${getErrorMessage(err)}`);
          }
        }
      );
    }),

    vscode.commands.registerCommand('atus.runInSandbox', async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) {
        vscode.window.showInformationMessage('Open a C# file to run in NET-Tutos Roslyn Sandbox.');
        return;
      }

      const code = !editor.selection.isEmpty ? editor.document.getText(editor.selection) : editor.document.getText();
      outputChannel.show(true);
      outputChannel.appendLine(`[${new Date().toLocaleTimeString()}] Running in NET-Tutos Roslyn Sandbox (${client.getBaseUrl()})...`);

      try {
        const res = await client.executeSandbox(code);
        outputChannel.appendLine(`Status: ${res.isSuccess ? 'SUCCESS' : res.isTimeout ? 'TIMEOUT' : 'ERROR'} (${res.executionTimeMs} ms)`);
        if (res.output) {
          outputChannel.appendLine('--- STDOUT ---');
          outputChannel.appendLine(res.output);
        }
        if (res.error) {
          outputChannel.appendLine('--- ERROR ---');
          outputChannel.appendLine(res.error);
        }
        outputChannel.appendLine('');
      } catch (err) {
        outputChannel.appendLine(`Request failed: ${getErrorMessage(err)}\n`);
        vscode.window.showErrorMessage(`Roslyn Sandbox failed: ${getErrorMessage(err)}`);
      }
    }),

    vscode.commands.registerCommand('atus.searchKnowledge', async () => {
      const query = await vscode.window.showInputBox({
        prompt: 'Search NET-Tutos Multi-Source Hybrid RAG (Curriculum, Tutorials, Challenges, Flashcards)',
        placeHolder: 'e.g., async await deadlock, EF Core AsNoTracking, Kafka outbox...'
      });
      if (!query || !query.trim()) return;

      try {
        const res = await client.searchKnowledge(query.trim(), 8);
        if (!res.hits || res.hits.length === 0) {
          vscode.window.showInformationMessage(`No matching NET-Tutos materials found for "${query}".`);
          return;
        }

        const picked = await vscode.window.showQuickPick(
          res.hits.map((h) => ({
            label: `[${h.sourceType}] ${h.title}`,
            description: `Score ${h.score.toFixed(3)}`,
            detail: h.summary,
            url: h.url
          })),
          {
            placeHolder: `${res.totalIndexedDocuments} indexed docs searched in ${res.elapsedMicroseconds} µs`
          }
        );

        if (picked?.url) {
          await vscode.env.openExternal(vscode.Uri.parse(`${client.getBaseUrl()}${picked.url}`));
        }
      } catch (err) {
        vscode.window.showErrorMessage(`RAG Search failed: ${getErrorMessage(err)}`);
      }
    }),

    vscode.commands.registerCommand('atus.applyLastRefactor', async () => {
      if (!lastRefactorState) {
        vscode.window.showInformationMessage('Run "Atus: Review C# Code & Big-O" first to generate a refactor.');
        return;
      }

      const editor = await vscode.window.showTextDocument(lastRefactorState.uri);
      const fullRange = new vscode.Range(
        editor.document.positionAt(0),
        editor.document.positionAt(editor.document.getText().length)
      );
      await editor.edit((eb) => eb.replace(fullRange, lastRefactorState!.code));
      vscode.window.showInformationMessage('Applied Atus .NET 10 refactor.');
    }),

    vscode.commands.registerCommand('atus.clearDiagnostics', () => {
      diagnostics.clear();
    }),

    vscode.commands.registerCommand('atus.signIn', async () => {
      const username = await vscode.window.showInputBox({
        prompt: 'NET-Tutos Username or Email',
        ignoreFocusOut: true
      });
      if (!username) return;

      const password = await vscode.window.showInputBox({
        prompt: 'NET-Tutos Password',
        password: true,
        ignoreFocusOut: true
      });
      if (!password) return;

      try {
        const auth = await client.login(username.trim(), password);
        if (!auth.isSuccess || !auth.token) {
          vscode.window.showErrorMessage(auth.message || 'Sign in failed.');
          return;
        }
        await context.secrets.store(AUTH_SECRET_KEY, auth.token);
        client.updateConfig({ authToken: auth.token });
        chatProvider.notifyConfigChanged();
        vscode.window.showInformationMessage(`Signed in to NET-Tutos as ${auth.user?.fullName || username}.`);
      } catch (err) {
        vscode.window.showErrorMessage(`Sign in failed: ${getErrorMessage(err)}`);
      }
    }),

    vscode.commands.registerCommand('atus.signOut', async () => {
      await context.secrets.delete(AUTH_SECRET_KEY);
      client.updateConfig({ authToken: null });
      chatProvider.notifyConfigChanged();
      vscode.window.showInformationMessage('Signed out of NET-Tutos Atus AI.');
    }),

    vscode.commands.registerCommand('atus.checkConnection', async () => {
      try {
        const status = await client.checkStatus();
        statusBar.text = '$(sparkle) Atus AI: Online';
        vscode.window.showInformationMessage(
          `Connected to ${status.service} (${status.indexedKnowledgeDocuments} RAG docs indexed).`
        );
      } catch (err) {
        statusBar.text = '$(warning) Atus AI: Offline';
        vscode.window.showWarningMessage(`Cannot reach ${client.getBaseUrl()}: ${getErrorMessage(err)}`);
      }
    })
  );

  // Silent non-blocking status check at startup
  void client
    .checkStatus()
    .then((s) => {
      statusBar.text = `$(sparkle) Atus AI (${s.indexedKnowledgeDocuments})`;
    })
    .catch(() => {
      statusBar.text = '$(sparkle) Atus AI (Offline)';
    });
}

export function deactivate(): void {
  // Subscriptions are disposed automatically by VS Code
}

async function createConfiguredClient(secrets: vscode.SecretStorage): Promise<AtusClient> {
  const settings = await readSettings();
  const token = await secrets.get(AUTH_SECRET_KEY);
  return new AtusClient({
    ...settings,
    authToken: token
  });
}

async function readSettings(): Promise<{ serverUrl: string; language: AtusLanguage; timeoutMs: number }> {
  const cfg = vscode.workspace.getConfiguration('atus');
  const rawLang = cfg.get<string>('language', 'vi');
  return {
    serverUrl: cfg.get<string>('serverUrl', 'http://localhost:5001'),
    language: rawLang === 'en' ? 'en' : 'vi',
    timeoutMs: cfg.get<number>('requestTimeoutMs', 25000)
  };
}

function getErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

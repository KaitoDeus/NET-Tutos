import * as vscode from 'vscode';

/**
 * Provides inline Quick Fix & Refactor CodeActions (lightbulb menu) in C# files:
 * - Explain compiler diagnostics on the current line using Atus AI
 * - Run Atus Clean Code & Big-O review on active selection
 * - Generate xUnit tests for selected method or class
 */
export class AtusCodeActionProvider implements vscode.CodeActionProvider {
  public static readonly providedCodeActionKinds = [
    vscode.CodeActionKind.QuickFix,
    vscode.CodeActionKind.Refactor
  ];

  public provideCodeActions(
    document: vscode.TextDocument,
    range: vscode.Range | vscode.Selection,
    context: vscode.CodeActionContext
  ): vscode.CodeAction[] {
    if (document.languageId !== 'csharp') {
      return [];
    }

    const actions: vscode.CodeAction[] = [];

    if (context.diagnostics.length > 0) {
      const firstDiag = context.diagnostics[0];
      const explainAction = new vscode.CodeAction(
        `Atus: Explain diagnostic "${truncate(firstDiag.message, 48)}"`,
        vscode.CodeActionKind.QuickFix
      );
      explainAction.diagnostics = [firstDiag];
      explainAction.isPreferred = false;
      explainAction.command = {
        command: 'atus.explainSelectionOrDiagnostics',
        title: 'Atus: Explain Diagnostic',
        arguments: [firstDiag.message, range]
      };
      actions.push(explainAction);
    }

    if (!range.isEmpty) {
      const reviewAction = new vscode.CodeAction(
        'Atus: Review Selection & Analyze Big-O',
        vscode.CodeActionKind.Refactor
      );
      reviewAction.command = {
        command: 'atus.reviewCurrentFile',
        title: 'Atus: Review C# Code & Big-O'
      };
      actions.push(reviewAction);

      const testAction = new vscode.CodeAction(
        'Atus: Generate xUnit Unit Tests for Selection',
        vscode.CodeActionKind.Refactor
      );
      testAction.command = {
        command: 'atus.generateUnitTests',
        title: 'Atus: Generate xUnit Tests'
      };
      actions.push(testAction);
    }

    return actions;
  }
}

function truncate(value: string, maxLen: number): string {
  if (value.length <= maxLen) return value;
  return `${value.slice(0, maxLen - 1)}…`;
}

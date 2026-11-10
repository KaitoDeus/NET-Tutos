import * as assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { IdeReviewBundleResponse } from '../atusClient';
import { formatReviewMarkdown, inferLineNumber, mapReviewToDiagnostics } from '../reviewMapper';

describe('reviewMapper', () => {
  const sampleSource = [
    'using System.Threading.Tasks;',
    'public class OrderService',
    '{',
    '    public string LoadSync(Task<string> task)',
    '    {',
    '        return task.Result;',
    '    }',
    '}'
  ].join('\n');

  const sampleBundle: IdeReviewBundleResponse = {
    success: true,
    fileName: 'OrderService.cs',
    review: {
      success: true,
      score: 74,
      summary: 'Blocking on async Task detected.',
      points: [
        {
          category: 'Async/Await',
          severity: 'warning',
          title: 'Avoid sync-over-async `task.Result`',
          description: 'Use await instead of blocking ThreadPool threads.'
        },
        {
          category: 'Readability',
          severity: 'success',
          title: 'Clean class naming',
          description: 'Good PascalCase naming.'
        }
      ],
      refactoredCode: 'public async Task<string> LoadAsync(Task<string> task) => await task;'
    },
    complexity: {
      success: true,
      timeComplexity: 'O(1)',
      spaceComplexity: 'O(1)',
      efficiencyScore: 95,
      summary: 'Constant work',
      detailedExplanation: 'Single await continuation',
      optimizationTip: 'Make caller async all the way.'
    }
  };

  it('infers matching 0-based line number from backtick symbol and skips success points', () => {
    const line = inferLineNumber(sampleSource, sampleBundle.review.points[0]);
    assert.equal(line, 5);

    const diags = mapReviewToDiagnostics(sampleSource, sampleBundle);
    assert.equal(diags.length, 1);
    assert.equal(diags[0].severity, 'warning');
    assert.equal(diags[0].line, 5);
    assert.match(diags[0].message, /\[Async\/Await\]/);
  });

  it('formats Vietnamese and English review markdown reports with refactored C# block', () => {
    const mdVi = formatReviewMarkdown(sampleBundle, 'vi');
    assert.match(mdVi, /74\/100/);
    assert.match(mdVi, /O\(1\)/);
    assert.match(mdVi, /```csharp/);

    const mdEn = formatReviewMarkdown(sampleBundle, 'en');
    assert.match(mdEn, /Clean Code Score/);
    assert.match(mdEn, /Suggested \.NET 10 Refactor/);
  });
});

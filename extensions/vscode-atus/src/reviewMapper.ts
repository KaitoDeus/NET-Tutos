import { AtusLanguage, IdeReviewBundleResponse, IdeReviewPoint } from './atusClient';

export interface MappedReviewDiagnostic {
  line: number;
  severity: 'error' | 'warning' | 'info';
  message: string;
  source: string;
}

export function mapSeverity(severity: string): 'error' | 'warning' | 'info' {
  const norm = (severity || '').toLowerCase();
  if (norm === 'error' || norm === 'critical' || norm === 'high') {
    return 'error';
  }
  if (norm === 'warning' || norm === 'medium') {
    return 'warning';
  }
  return 'info';
}

/**
 * Heuristically locates the most relevant 0-based line index inside C# source
 * for an Atus review point so inline editor squiggles land near the target construct.
 */
export function inferLineNumber(sourceCode: string, point: IdeReviewPoint): number {
  const lines = (sourceCode || '').split(/\r?\n/);
  if (lines.length === 0) return 0;

  const lineMatch = `${point.title} ${point.description}`.match(/(?:line|dòng)\s*[:#]?\s*(\d+)/i);
  if (lineMatch) {
    const parsed = Number.parseInt(lineMatch[1], 10);
    if (!Number.isNaN(parsed) && parsed >= 1 && parsed <= lines.length) {
      return parsed - 1;
    }
  }

  const backtickTokens = Array.from(`${point.title} ${point.description}`.matchAll(/`([A-Za-z0-9_.<>]+)`/g))
    .map((m) => m[1])
    .filter((token) => token.length >= 3);

  for (const token of backtickTokens) {
    const idx = lines.findIndex((ln) => ln.includes(token));
    if (idx >= 0) {
      return idx;
    }
  }

  const categoryLower = (point.category || '').toLowerCase();
  if (categoryLower.includes('async') || point.title.toLowerCase().includes('async')) {
    const idx = lines.findIndex((ln) => /\b(async|Task|\.Result|\.Wait\()\b/.test(ln));
    if (idx >= 0) return idx;
  }

  return 0;
}

export function mapReviewToDiagnostics(sourceCode: string, bundle: IdeReviewBundleResponse): MappedReviewDiagnostic[] {
  const diagnostics: MappedReviewDiagnostic[] = [];
  const points = bundle.review?.points ?? [];

  for (const point of points) {
    const sev = mapSeverity(point.severity);
    // Do not draw noisy squiggles for purely positive "success" remarks
    if ((point.severity || '').toLowerCase() === 'success') {
      continue;
    }

    diagnostics.push({
      line: inferLineNumber(sourceCode, point),
      severity: sev,
      message: `[${point.category}] ${point.title}: ${point.description}`,
      source: 'Atus AI (.NET 10)'
    });
  }

  return diagnostics;
}

export function formatReviewMarkdown(bundle: IdeReviewBundleResponse, lang: AtusLanguage): string {
  const isEn = lang === 'en';
  const r = bundle.review;
  const c = bundle.complexity;

  const lines: string[] = [
    `### ${isEn ? 'Atus Code Review & Big-O Analysis' : 'Atus Đánh Giá Code & Độ Phức Tạp Big-O'} — \`${bundle.fileName}\``,
    '',
    `- **${isEn ? 'Clean Code Score' : 'Điểm Clean Code'}:** \`${r.score}/100\``,
    `- **${isEn ? 'Efficiency Score' : 'Điểm Hiệu Năng'}:** \`${c.efficiencyScore}/100\` | **Time:** \`${c.timeComplexity}\` | **Space:** \`${c.spaceComplexity}\``,
    '',
    `**${isEn ? 'Summary' : 'Tóm tắt'}:** ${r.summary}`,
    ''
  ];

  if (r.points?.length > 0) {
    lines.push(`#### ${isEn ? 'Key Review Findings' : 'Nhận Xét Chi Tiết'}`);
    for (const pt of r.points) {
      const icon = pt.severity === 'warning' ? '⚠️' : pt.severity === 'error' ? '🔴' : '💡';
      lines.push(`- ${icon} **[${pt.category}] ${pt.title}** — ${pt.description}`);
    }
    lines.push('');
  }

  if (c.optimizationTip) {
    lines.push(`> **${isEn ? 'Optimization Tip' : 'Gợi ý Tối ưu'}:** ${c.optimizationTip}`);
    lines.push('');
  }

  if (r.refactoredCode && r.refactoredCode.trim().length > 0) {
    lines.push(`#### ${isEn ? 'Suggested .NET 10 Refactor' : 'Mã Nguồn Đề Xuất (.NET 10)'}`);
    lines.push('```csharp');
    lines.push(r.refactoredCode.trim());
    lines.push('```');
  }

  return lines.join('\n');
}

export function escapeHtml(raw: string): string {
  return (raw || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

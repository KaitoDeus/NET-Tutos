export type AtusLanguage = 'vi' | 'en';

export interface IdeKnowledgeHit {
  sourceType: string;
  title: string;
  summary: string;
  url: string;
  score: number;
}

export interface IdeKnowledgeSearchResponse {
  query: string;
  language: string;
  totalIndexedDocuments: number;
  sourceCounts: Record<string, number>;
  elapsedMicroseconds: number;
  hits: IdeKnowledgeHit[];
}

export interface IdeStatusResponse {
  success: boolean;
  service: string;
  agent: string;
  indexedKnowledgeDocuments: number;
  authenticatedUserId?: string | null;
  serverTimestampUtc: string;
  aiStatus?: {
    isConfigured?: boolean;
    model?: string;
    message?: string;
    activeEngine?: string;
  };
}

export interface IdeChatResponse {
  success: boolean;
  modelUsed: string;
  responseMarkdown: string;
  codeSnippet?: string | null;
  suggestedFollowUps?: string[];
  references?: IdeKnowledgeHit[];
  timestamp?: string;
}

export interface IdeReviewPoint {
  category: string;
  severity: 'info' | 'warning' | 'error' | 'success' | string;
  title: string;
  description: string;
}

export interface IdeCodeReviewResponse {
  success: boolean;
  score: number;
  summary: string;
  points: IdeReviewPoint[];
  refactoredCode?: string | null;
}

export interface IdeComplexityResponse {
  success: boolean;
  timeComplexity: string;
  spaceComplexity: string;
  efficiencyScore: number;
  summary: string;
  detailedExplanation: string;
  optimizationTip?: string | null;
  optimalComparison?: string | null;
}

export interface IdeReviewBundleResponse {
  success: boolean;
  fileName: string;
  review: IdeCodeReviewResponse;
  complexity: IdeComplexityResponse;
}

export interface IdeExplainErrorResponse {
  success: boolean;
  errorCode?: string | null;
  lineNumber?: number | null;
  errorTitle: string;
  explanation: string;
  howToFix: string;
  suggestedFixCode?: string | null;
  proTip?: string | null;
}

export interface IdeGenerateUnitTestResponse {
  success: boolean;
  testFramework: string;
  testCode: string;
  explanation: string;
  coveredScenarios: string[];
}

export interface IdeExecuteResponse {
  isSuccess: boolean;
  output: string;
  error?: string | null;
  executionTimeMs: number;
  isTimeout: boolean;
}

export interface IdeLoginResponse {
  isSuccess: boolean;
  message: string;
  token?: string | null;
  user?: {
    username: string;
    fullName: string;
    totalXp: number;
    level: number;
    streakDays: number;
  };
}

export interface AtusClientConfig {
  serverUrl: string;
  language: AtusLanguage;
  timeoutMs: number;
  authToken?: string | null;
}

export class AtusApiError extends Error {
  public readonly statusCode: number;

  constructor(message: string, statusCode: number) {
    super(message);
    this.name = 'AtusApiError';
    this.statusCode = statusCode;
  }
}

export class AtusClient {
  private config: AtusClientConfig;

  constructor(config: AtusClientConfig) {
    this.config = {
      ...config,
      serverUrl: AtusClient.normalizeBaseUrl(config.serverUrl)
    };
  }

  public updateConfig(partial: Partial<AtusClientConfig>): void {
    this.config = {
      ...this.config,
      ...partial,
      serverUrl: AtusClient.normalizeBaseUrl(partial.serverUrl ?? this.config.serverUrl)
    };
  }

  public getBaseUrl(): string {
    return this.config.serverUrl;
  }

  public getLanguage(): AtusLanguage {
    return this.config.language;
  }

  public isAuthenticated(): boolean {
    return Boolean(this.config.authToken && this.config.authToken.trim().length > 0);
  }

  public static normalizeBaseUrl(url: string): string {
    const trimmed = (url || 'http://localhost:5001').trim();
    return trimmed.replace(/\/+$/, '');
  }

  public async checkStatus(): Promise<IdeStatusResponse> {
    return this.request<IdeStatusResponse>('GET', '/api/v1/ide/status');
  }

  public async searchKnowledge(query: string, top = 6, lang?: AtusLanguage): Promise<IdeKnowledgeSearchResponse> {
    const effectiveLang = lang ?? this.config.language;
    const qs = new URLSearchParams({
      q: query ?? '',
      top: String(top),
      lang: effectiveLang
    });
    return this.request<IdeKnowledgeSearchResponse>('GET', `/api/v1/ide/search?${qs.toString()}`);
  }

  public async chat(params: {
    message: string;
    codeContext?: string;
    fileName?: string;
    errorContext?: string;
    lang?: AtusLanguage;
  }): Promise<IdeChatResponse> {
    return this.request<IdeChatResponse>('POST', '/api/v1/ide/chat', {
      message: params.message,
      codeContext: params.codeContext ?? null,
      fileName: params.fileName ?? null,
      errorContext: params.errorContext ?? null,
      lang: params.lang ?? this.config.language
    });
  }

  public async reviewCode(params: {
    sourceCode: string;
    fileName?: string;
    context?: string;
    lang?: AtusLanguage;
  }): Promise<IdeReviewBundleResponse> {
    return this.request<IdeReviewBundleResponse>('POST', '/api/v1/ide/review', {
      sourceCode: params.sourceCode,
      fileName: params.fileName ?? null,
      context: params.context ?? null,
      lang: params.lang ?? this.config.language
    });
  }

  public async explainError(params: {
    errorMessage: string;
    sourceCode?: string;
    fileName?: string;
    lang?: AtusLanguage;
  }): Promise<IdeExplainErrorResponse> {
    return this.request<IdeExplainErrorResponse>('POST', '/api/v1/ide/explain-error', {
      errorMessage: params.errorMessage,
      sourceCode: params.sourceCode ?? '',
      fileName: params.fileName ?? null,
      lang: params.lang ?? this.config.language
    });
  }

  public async generateUnitTests(params: {
    sourceCode: string;
    fileName?: string;
    lang?: AtusLanguage;
  }): Promise<IdeGenerateUnitTestResponse> {
    return this.request<IdeGenerateUnitTestResponse>('POST', '/api/v1/ide/generate-tests', {
      sourceCode: params.sourceCode,
      fileName: params.fileName ?? null,
      lang: params.lang ?? this.config.language
    });
  }

  public async executeSandbox(code: string, timeoutMs = 4000): Promise<IdeExecuteResponse> {
    return this.request<IdeExecuteResponse>('POST', '/api/v1/ide/execute', {
      code,
      timeoutMs
    });
  }

  public async login(usernameOrEmail: string, password: string): Promise<IdeLoginResponse> {
    return this.request<IdeLoginResponse>('POST', '/api/auth/login', {
      usernameOrEmail,
      password
    });
  }

  private async request<T>(method: 'GET' | 'POST', path: string, body?: unknown): Promise<T> {
    const url = `${this.config.serverUrl}${path}`;
    const headers: Record<string, string> = {
      Accept: 'application/json',
      'Accept-Language': this.config.language === 'en' ? 'en-US' : 'vi-VN'
    };

    if (body !== undefined) {
      headers['Content-Type'] = 'application/json';
    }

    if (this.config.authToken) {
      headers['Authorization'] = `Bearer ${this.config.authToken}`;
    }

    const response = await fetch(url, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal: AbortSignal.timeout(Math.max(1000, this.config.timeoutMs))
    });

    const text = await response.text();
    let parsed: unknown = null;
    if (text) {
      try {
        parsed = JSON.parse(text);
      } catch {
        parsed = null;
      }
    }

    if (!response.ok) {
      const msg =
        parsed && typeof parsed === 'object' && 'message' in parsed && typeof (parsed as { message: unknown }).message === 'string'
          ? (parsed as { message: string }).message
          : `HTTP ${response.status} (${response.statusText})`;
      throw new AtusApiError(msg, response.status);
    }

    if (parsed === null) {
      throw new AtusApiError(`Invalid JSON response from ${path}`, response.status);
    }

    return parsed as T;
  }
}

import * as assert from 'node:assert/strict';
import * as http from 'node:http';
import { after, before, describe, it } from 'node:test';
import { AtusApiError, AtusClient } from '../atusClient';

describe('AtusClient', () => {
  let server: http.Server;
  let baseUrl = '';
  let lastAuthHeader = '';
  let lastLangHeader = '';
  let lastBody = '';

  before(async () => {
    server = http.createServer(async (req, res) => {
      lastAuthHeader = String(req.headers.authorization || '');
      lastLangHeader = String(req.headers['accept-language'] || '');

      const chunks: Buffer[] = [];
      for await (const chunk of req) {
        chunks.push(Buffer.from(chunk));
      }
      lastBody = Buffer.concat(chunks).toString('utf8');

      res.setHeader('Content-Type', 'application/json');

      if (req.url?.startsWith('/api/v1/ide/status')) {
        res.end(
          JSON.stringify({
            success: true,
            service: 'NET-Tutos.IDE.Bridge',
            agent: 'Atus',
            indexedKnowledgeDocuments: 175,
            serverTimestampUtc: '2026-11-10T13:00:00Z'
          })
        );
        return;
      }

      if (req.url?.startsWith('/api/v1/ide/review') && req.method === 'POST') {
        res.end(
          JSON.stringify({
            success: true,
            fileName: 'Demo.cs',
            review: { score: 92, summary: 'Clean async code', points: [] },
            complexity: {
              success: true,
              timeComplexity: 'O(N)',
              spaceComplexity: 'O(1)',
              efficiencyScore: 90,
              summary: 'Linear scan',
              detailedExplanation: 'Single loop'
            }
          })
        );
        return;
      }

      if (req.url?.startsWith('/api/v1/ide/execute') && req.method === 'POST') {
        res.statusCode = 400;
        res.end(JSON.stringify({ isSuccess: false, message: 'Source code cannot be empty.' }));
        return;
      }

      res.statusCode = 404;
      res.end(JSON.stringify({ message: 'Not found' }));
    });

    await new Promise<void>((resolve) => {
      server.listen(0, '127.0.0.1', () => {
        const addr = server.address();
        if (addr && typeof addr === 'object') {
          baseUrl = `http://127.0.0.1:${addr.port}`;
        }
        resolve();
      });
    });
  });

  after(async () => {
    await new Promise<void>((resolve) => server.close(() => resolve()));
  });

  it('normalizes trailing slash and sends auth + language headers', async () => {
    const client = new AtusClient({
      serverUrl: `${baseUrl}///`,
      language: 'vi',
      timeoutMs: 5000,
      authToken: 'jwt-sample-token'
    });

    assert.equal(client.getBaseUrl(), baseUrl);
    assert.equal(client.isAuthenticated(), true);

    const status = await client.checkStatus();
    assert.equal(status.success, true);
    assert.equal(status.indexedKnowledgeDocuments, 175);
    assert.equal(lastAuthHeader, 'Bearer jwt-sample-token');
    assert.equal(lastLangHeader, 'vi-VN');
  });

  it('posts review payload with updated English language setting', async () => {
    const client = new AtusClient({
      serverUrl: baseUrl,
      language: 'en',
      timeoutMs: 5000
    });

    const bundle = await client.reviewCode({
      sourceCode: 'Console.WriteLine("Hi");',
      fileName: 'Demo.cs'
    });

    assert.equal(lastLangHeader, 'en-US');
    assert.equal(bundle.review.score, 92);
    assert.equal(bundle.complexity.timeComplexity, 'O(N)');
    const sent = JSON.parse(lastBody) as { lang: string; fileName: string };
    assert.equal(sent.lang, 'en');
    assert.equal(sent.fileName, 'Demo.cs');
  });

  it('throws AtusApiError on non-2xx API responses', async () => {
    const client = new AtusClient({
      serverUrl: baseUrl,
      language: 'vi',
      timeoutMs: 5000
    });

    await assert.rejects(
      async () => client.executeSandbox(''),
      (err: unknown) =>
        err instanceof AtusApiError &&
        err.statusCode === 400 &&
        err.message === 'Source code cannot be empty.'
    );
  });
});

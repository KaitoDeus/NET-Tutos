# Atus AI — NET-Tutos .NET 10 Pair Programmer (VS Code Extension)

AI pair-programming assistant for C# and .NET 10 connecting directly to the **NET-Tutos** platform (`Web BFF` or `YARP Gateway`).

## Key Features

- **Sidebar Chat with Atus (`atus.chatView`)**: Ask C#, ASP.NET Core, EF Core, and Kafka questions with active C# file context and citations powered by **Multi-Source Hybrid RAG** (`Curriculum`, `Tutorials`, `Challenges`, `Flashcards`).
- **Inline Code Review & Big-O Analysis (`atus.reviewCurrentFile`)**: Highlights Clean Code and performance findings directly inside the editor (`DiagnosticCollection`), reports Big-O Time/Space complexity, and provides 1-click `.NET 10` refactoring (`atus.applyLastRefactor`).
- **Explain Error / Diagnostics (`atus.explainSelectionOrDiagnostics`)**: Lightbulb Quick Fix that inspects compiler diagnostics at the cursor line and proposes targeted code fixes.
- **Generate xUnit Unit Tests (`atus.generateUnitTests`)**: Synthesizes AAA-structured `xUnit` test suites into an adjacent editor tab.
- **Run in NET-Tutos Roslyn Sandbox (`atus.runInSandbox`)**: Executes selected or full C# snippets in the NET-Tutos Roslyn Sandbox and streams output to the VS Code Output Channel.
- **RAG Knowledge Search (`atus.searchKnowledge`)**: QuickPick instant search across 175+ indexed .NET curriculum documents in the platform.

## Installation & Running in Development Host

```bash
cd extensions/vscode-atus
npm install
npm run compile
npm test
```

Then open the `extensions/vscode-atus` folder in VS Code and press **F5** to launch the Extension Development Host.

## Configuration Settings

| Setting | Default | Description |
|---|---|---|
| `atus.serverUrl` | `http://localhost:5001` | Base URL of NET-Tutos Web BFF or YARP Gateway (`http://localhost:5000`) |
| `atus.language` | `vi` | Response language (`vi` or `en`) |
| `atus.autoReviewOnSave` | `false` | Automatically run inline Clean Code review on `.cs` file save |
| `atus.requestTimeoutMs` | `25000` | HTTP request timeout in milliseconds |

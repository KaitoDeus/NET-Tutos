# Design System & Anti-AI-Slop Engineering Guidelines

Document Type: Frontend UI/UX Design System Specification & Quality Constitution  
Author: Senior Principal Product Designer & Lead Frontend Architect  
System: .NET Tutos - Comprehensive .NET Learning & Assessment Platform  
Scope: Web Application (ASP.NET Core 10), Razor Presentation Views, CSS Architecture, Mobile App Alignment  
Version: Release 3.0  
Status: Verified & Adopted as Canonical Frontend Constitution  

---

## 1. Core Philosophy: The Developer Tool Standard

Modern software engineers spend their days in high-craft, dense, intentional tools: **Linear, Raycast, Stripe, GitHub, VS Code, and Vercel**. 

These tools share a design language grounded in **utility, legibility, and restraint**. In contrast, AI-generated interfaces ("AI Slop") chronically suffer from visual cliches: gratuitous purple-to-blue neon gradients, oversized bubble cards, floating 3D isometric blobs, and low-contrast muted text.

```
┌──────────────────────────────────────────────┐     ┌──────────────────────────────────────────────┐
│        AI-GENERATED "SLOP" CLICHÉS           │     │       HANDCRAFTED DEV-TOOL STANDARD          │
├──────────────────────────────────────────────┤     ├──────────────────────────────────────────────┤
│ ❌ Neon purple/cyan multi-stop gradients     │ ──> │ ✅ Grounded neutral canvas (Zinc/Slate)      │
│ ❌ Bubble border radii (16px–24px, pill-all) │ ──> │ ✅ Crisp, disciplined radii (6px–8px)        │
│ ❌ Nested card-in-a-card syndrome            │ ──> │ ✅ 1px subtle borders (border-subtle)        │
│ ❌ Fake 3D isometric floating polygons       │ ──> │ ✅ Real, functional C# 14 code & terminals   │
│ ❌ Bouncy/elastic hover transforms (rotate)  │ ──> │ ✅ Instant tactile feedback (100ms–150ms)    │
│ ❌ Low-contrast faint gray text on dark      │ ──> │ ✅ High contrast (WCAG AAA compliant text)   │
│ ❌ Meaningless marketing buzzwords           │ ──> │ ✅ Direct, task-oriented technical copy      │
└──────────────────────────────────────────────┘     └──────────────────────────────────────────────┘
```

The guiding principle of .NET Tutos is: **"Every pixel must earn its right to exist through functional utility."**

---

## 2. Banned Design Patterns (Anti-Slop Strict Exclusions)

The following patterns are **strictly prohibited** in all templates, views, and styles across `.NET Tutos`:

1. **Banned: Decorative Neon Gradients**
   - **Never** apply multi-stop linear gradients across large surfaces, background canvases, or hero typography (`linear-gradient(135deg, #512bd4, #2b78e4)`).
   - Solid, purposeful colors must be used. Backgrounds must be calm neutrals.
2. **Banned: Bubble & Pill Overuse**
   - **Never** use `border-radius: 1rem` (16px), `1.5rem` (24px), or full pills for standard cards, dialogs, dropdowns, and buttons.
   - Radii must be disciplined: `4px` (`sm`), `6px` (`md`), `8px` (`lg`).
3. **Banned: Card Nesting & Heavy Blurry Shadows**
   - **Never** nest cards inside cards with overlapping soft drop shadows (`box-shadow: 0 20px 45px ...`).
   - Group related elements using **1px hairline borders** (`border: 1px solid var(--border-subtle)`) and background color differentiation.
4. **Banned: Cartoonish & Bouncy Motion**
   - **Never** use spring, bounce, or rotation hover effects (e.g., `transform: scale(1.1) rotate(4deg)`).
   - Micro-interactions must be fast and tactile: border-color shifts, subtle background highlights, or max 1px translation.
5. **Banned: Fake 3D Isometric Graphics & Empty Illustrations**
   - **Never** include floating geometric SVG polygons, isometric cubes, or generic AI stock illustrations that convey zero domain information.
   - Visual interest must come from **authentic C# 14 code snippets, interactive terminals, diff viewers, and real data charts**.
6. **Banned: Low-Contrast Faint Grays**
   - Faint secondary text that hurts legibility is banned. All body and instructional copy must pass WCAG AA (minimum 4.5:1) and target WCAG AAA (7:1).

---

## 3. Design Tokens & Visual Hierarchy

### 3.1. Neutral Palette (Grounded Zinc / Slate)

| Token Name | Light Mode Value | Dark Mode Value | Usage |
| :--- | :--- | :--- | :--- |
| `--bg-canvas` | `#f8fafc` (Slate 50) | `#09090b` (Deep Zinc 950) | Full page background |
| `--bg-surface` | `#ffffff` | `#121215` (Solid elevated surface) | Main cards, modals, sidebars |
| `--bg-subtle` | `#f1f5f9` (Slate 100) | `#18181b` (Zinc 900) | Secondary panels, code blocks |
| `--bg-hover` | `#e2e8f0` (Slate 200) | `#27272a` (Zinc 800) | Row hovers, button hovers |
| `--border-subtle` | `#e2e8f0` | `#27272a` (Crisp 1px border) | Default card & section divider |
| `--border-strong` | `#cbd5e1` | `#3f3f46` | Input borders, active tab borders |
| `--text-primary` | `#0f172a` (High-contrast slate) | `#f4f4f5` (Crisp zinc white) | Headings, primary content |
| `--text-secondary`| `#475569` | `#a1a1aa` | Descriptions, metadata |
| `--text-muted` | `#64748b` | `#71717a` | Timestamps, minor footnotes |

### 3.2. Brand & Semantic Accent Tokens

The platform uses authentic **Microsoft .NET Indigo (`#512BD4`)** as its deliberate, single-hue accent.

| Token Name | Value | Purpose |
| :--- | :--- | :--- |
| `--dotnet-primary` | `#512bd4` | Brand mark, primary CTA button, active stepper state |
| `--dotnet-primary-hover`| `#4323b0` | Tactile pressed / hovered state for primary actions |
| `--dotnet-subtle` | `rgba(81, 43, 212, 0.08)` | Active row highlight, badge background |
| `--color-success` | `#10b981` | Passing unit tests, accepted solutions, streaks |
| `--color-warning` | `#f59e0b` | XP points, tips, intermediate difficulty |
| `--color-danger` | `#ef4444` | Roslyn compilation errors, failed test cases |
| `--color-info` | `#0284c7` | Advanced architecture callouts, hints |

---

## 4. Typography Standards & Strict Type Scale

```
Display:  Montserrat, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif
Code/Kbd: "JetBrains Mono", "Fira Code", monospace
```

### 4.1. Standardized Type Scale & Token Specifications (Tuned for Montserrat)

| Token | Size (Rem / Px) | Line Height | Tracking | Weight | Semantic Use Case |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `--text-4xl` | `2.1rem` (33.6px) | `1.2` | `-0.025em` | `700` (Bold) | Maximum hero title display |
| `--text-3xl` | `1.75rem` (28px) | `1.25` | `-0.02em` | `700` (Bold) | Major page headers (`H1`, `.display-5`) |
| `--text-2xl` | `1.45rem` (23.2px) | `1.3` | `-0.015em` | `700` (Bold) | Section headers (`H2`, `.display-6`) |
| `--text-xl` | `1.25rem` (20px) | `1.35` | `-0.015em` | `600` (SemiBold) | Section sub-headers (`H3`) |
| `--text-lg` | `1.125rem` (18px) | `1.4` | `-0.01em` | `600` (SemiBold) | Card group headers, section titles (`H4`) |
| `--text-md` | `1rem` (16px) | `1.55` | `0em` | `400` / `600` | Hero subtitle (`.hero-subtitle`), card titles (`H5`) |
| `--text-base` | `0.9375rem` (15px) | `1.65` | `0em` | `400` (Normal) | Default body text & Markdown tutorial reading |
| `--text-nav` | `0.875rem` (14px) | `1.4` | `-0.005em` | `500` / `600` | Top navbar links, action buttons (`.btn`) |
| `--text-sm` | `0.875rem` (14px) | `1.5` | `0em` | `400` / `600` | Card summaries, roadmap stage titles (`H6`), list items |
| `--text-xs` | `0.8125rem` (13px) | `1.4` | `0.02em` | `500` / `600` | Section labels (`.section-label`), timestamps, lesson counts |
| `--text-2xs` | `0.75rem` (12px) | `1.25` | `0.035em` | `600` (SemiBold) | Minimum micro badge indicators, status pills |

### 4.2. Core Typographic Rules
- **No Arbitrary Ultra-Heavy Weights:** Banned `font-weight: 800` and `900`. All bold headings use `font-weight: 700`.
- **Prose Readability:** Tutorial body text enforces `line-height: 1.65` to `1.7` for optimal reading comprehension with geometric letterforms.
- **Monospace Elements (Code, Badges, Metrics, Shortcuts):** Wrapped in `<kbd>` or `.font-monospace` with exact `0.8125rem` (13px) sizing and `0em` tracking.
- **Calibrated Display Classes:** Bootstrap `.display-1` through `.display-6` are capped between `1.2rem` (19.2px) and `2.75rem` (44px) to prevent layout breakages.

---

## 5. Component Anatomy & Engineering Ergonomics

### 5.1. Hero Section: Code-First Developer Terminal
Instead of an empty marketing header with stock graphics, the Hero section presents a **functional, interactive C# 14 code preview**:
- Terminal header with 3 window controls (`red`, `yellow`, `green` subtle dots).
- Tab bar showing active file (`Program.cs`) and language pill (`C# 14 | .NET 10`).
- Crisp syntax-highlighted code demonstrating modern C# features (e.g. pattern matching, LINQ, records).
- Direct actions: *"Run in Playground"*, *"Start Lesson 1"*, *"Explore Roadmap"*.

### 5.2. Cards & Modular Containers
- Max `border-radius: 8px`.
- Hairline `1px solid var(--border-subtle)`.
- No loud drop shadows: `box-shadow: 0 1px 2px rgba(0,0,0,0.04)`.
- Hover state: Border shifts to `--border-strong` or `--dotnet-primary`, background lightens/darkens subtly, `transform: translateY(-1px)` maximum.

### 5.3. Status Badges & Chips
- Use the **Dot + Label format**:
  - `● Active` (Green dot + dark text)
  - `● Easy / Medium / Hard` (Colored dot + compact text)
- Avoid giant rounded bubble badges with thick shadows.

---

## 6. Anti-Slop Acceptance Checklist (Review Gate)

All templates, Razor views, styles, and UI components across `.NET Tutos` have been audited against this 6-point quality checklist:

- [x] **No gradient text or rainbow gradient backgrounds:** All surfaces are solid and grounded in the neutral Zinc/Slate palette (`#f8fafc` / `#09090b`). Multistop gradients and gradient texts have been eliminated.
- [x] **Disciplined radii:** All cards, containers, dialogs, and inputs enforce disciplined 4px to 8px border-radius (`--radius-sm`, `--radius-md`, `--radius-lg`). Oversized 16px–24px pillows are eliminated.
- [x] **Hairline borders over blurry shadows:** Surfaces are defined by crisp 1px subtle hairline borders (`border: 1px solid var(--bs-border-color)`) and restrained elevation tokens. Deep, multi-layered blurred drop shadows are removed.
- [x] **Zero cartoonish animations:** Hover interactions are snappy and tactile (100ms–150ms). Bouncing transforms, elastic springs, and rotating keyframes have been replaced with subtle 1px translations or border-color shifts.
- [x] **Code-first utility:** Layouts prioritize functional C# 14 syntax, Roslyn runner terminal, structured curriculum progress, and keyboard shortcuts (`Ctrl+K`) over marketing fluff.
- [x] **Contrast check:** All typography and secondary text adhere to WCAG AA/AAA legibility standards across both Light and Dark themes, using calibrated Montserrat scale with minimum 12px for badges and 15px for body prose.

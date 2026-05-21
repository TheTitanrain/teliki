# Teliki Website Design Spec

**Date:** 2026-05-21  
**Status:** Approved

## Context

Teliki has no public-facing website. The goal is a GitHub Pages landing page that explains what the program does, shows key features, and lets visitors download or find the GitHub repo. No CMS, no build pipeline — just a static file pushed alongside the source code.

## Decisions

| Question | Answer |
|---|---|
| Language | English |
| Style | Bold / Marketing — dark gradient (indigo/purple palette) |
| Structure | Single scrollable page |
| Tech | Pure HTML + Tailwind CSS via CDN (no build step) |
| Deployment | GitHub Pages, served from `/docs` folder on `master` branch |
| Visuals | CSS mockup of player window in hero; SVG/emoji icons for features |

## Page Structure

Sections in order:

1. **Hero** — headline "Your screens. Your content.", tagline, CSS player mockup on the right, two CTAs: Download (primary) + GitHub (secondary)
2. **How it works** — 3-step flow: Drop media → Teliki scans & caches → Plays fullscreen
3. **Features** — 6-card grid: multi-monitor, auto-scan, local cache, failure resilient, images & video, INI config
4. **System requirements** — badge list: Windows 7 SP1+, .NET 4.7.2, Windows Media Player, display(s)
5. **Download / Get started** — large CTA, 4-step quick install instructions
6. **Footer** — MIT license, GitHub link

## Visual Design

- **Background:** `#0f0c29` (near-black indigo)
- **Hero gradient:** `135deg, #0f0c29 → #302b63 → #24243e`
- **Accent:** `#a78bfa` (light violet) for labels and highlights
- **Primary button:** gradient `#6d28d9 → #4f46e5`
- **Secondary button:** outlined in `#6d28d9`, text `#a78bfa`
- **Feature cards:** `rgba(109,40,217,0.08)` background, `rgba(109,40,217,0.2)` border
- **Section dividers:** `linear-gradient(90deg, transparent, rgba(109,40,217,0.4), transparent)`

### CSS Player Mockup (Hero)

Fake WinForms window showing:
- Titlebar with window-control dots and "Teliki — Display 1 of 2"
- Dark screen with centered filename, resolution, interval info
- Playlist position indicator (e.g., ▶ 3 / 12)
- Purple glow shadow

## File Layout

```
docs/
  index.html       ← entire site, single file
```

GitHub Pages settings: **Source = master branch, /docs folder**

## Links

- Download button → GitHub Releases page: `https://github.com/TheTitanrain/teliki/releases`
- GitHub button → `https://github.com/TheTitanrain/teliki`
- Footer GitHub link → same

## Verification

1. Open `docs/index.html` directly in browser (file://) — full page renders correctly
2. All sections visible on scroll
3. Links point to correct GitHub URLs
4. Responsive: page readable at 1024px and 1440px width
5. Push to `master`, configure GitHub Pages → `/docs`, verify live URL loads

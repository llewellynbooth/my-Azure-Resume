# 0007 — Frontend is hand-authored: no framework, no build step

**Status:** Accepted &nbsp;·&nbsp; **Date:** 2026-09

## Context

The site was a 2013 résumé template: jQuery 1.10.2, a 190 KB `plugins.js`,
Flexslider, Modernizr, four stylesheets (~230 KB), eight lorem-ipsum "portfolio"
modals, and a PositiveSSL trust-seal script using `document.write`. The content
(seven roles, education, certs) was worth keeping; the wrapper was not.

## Decision

Rebuild as one semantic `index.html` + one stylesheet (`css/site.css`) + one
vanilla `js/main.js`. No framework, no bundler, no build step — it deploys as-is
to the Storage `$web` container.

- CSS custom properties, fluid `clamp()` type, grid/flex, light + dark with a
  persisted toggle and a pre-paint bootstrap, `prefers-reduced-motion` honoured.
- Inline-SVG icons — no icon font.
- Progressive: all content is in the HTML; JS adds the counter, nav, scroll-spy,
  reveal and the contact form.

## Consequences

- ~450 KB of template JS/CSS removed; nothing to keep patched.
- No dependency graph to audit for the frontend.
- Trade-off: no component model or design-system tooling — acceptable for a
  single-page site, revisit only if it grows.
- A light pipeline (lint + minify + Lighthouse CI) and a move to Azure Static Web
  Apps (headers, PR previews, managed TLS) are the natural next steps.

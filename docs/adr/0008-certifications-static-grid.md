# 0008 — Certifications shown as a static grid, not Credly embeds

**Status:** Accepted &nbsp;·&nbsp; **Date:** 2026-09

## Context

The certifications section embedded 12 live Credly badge widgets (12 iframes +
`cdn.credly.com/embed.js`). Microsoft has ended its partnership with Credly, so
the Microsoft badges render with an **expired / partnership-ended** state — a bad
look on a résumé, and the single heaviest thing on the page.

## Decision

Replace the embeds with a hand-authored responsive card grid — certification
name, issuer, and a level pill (Expert / Associate / Fundamentals / Cisco). Names
were resolved from the original Credly badge IDs. A "Verify on Credly" link
remains for provenance.

## Consequences

- No third-party script or iframes; the section is fast, indexable by search
  engines, and themeable.
- The list is now maintained by hand — update `index.html` when a certification is
  added or lapses.
- Considered a single static image instead; rejected because image text isn't
  responsive, selectable, or indexable, and it breaks if the file goes missing.

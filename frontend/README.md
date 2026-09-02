# frontend/

Static site deployed to the Azure Storage `$web` container and fronted by CDN.
Single `index.html` + `css/site.css` + `js/main.js`. No build step.

- `main.js` — visitor counter (POST), theme toggle, mobile nav, scroll-spy,
  lazy-loaded Credly badges, contact form (POST /api/contact).

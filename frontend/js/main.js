/* Llewellyn Booth — résumé site
   Counter, theme toggle, nav, scroll reveal, lazy Credly, contact form. */

const API = 'https://resumefunctionapp-win-cqczeqc6d5gtdfbb.australiaeast-01.azurewebsites.net/api';

/* ---- theme ---------------------------------------------------------- */
const themeToggle = document.getElementById('theme-toggle');
function currentTheme() {
  return document.documentElement.getAttribute('data-theme')
    || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
}
themeToggle?.addEventListener('click', () => {
  const next = currentTheme() === 'dark' ? 'light' : 'dark';
  document.documentElement.setAttribute('data-theme', next);
  try { localStorage.setItem('theme', next); } catch { /* private mode */ }
});

/* ---- visitor counter (POST increments) ----------------------------- */
(async () => {
  const el = document.getElementById('counter');
  if (!el) return;
  try {
    const res = await fetch(`${API}/getResumeFunction`, {
      method: 'POST',
      headers: { 'Accept': 'application/json' }
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    el.textContent = Number(data.count ?? 0).toLocaleString();
  } catch (err) {
    console.error('visitor count:', err);
    el.textContent = 'N/A';
    el.title = 'Unable to load visitor count';
  }
})();

/* ---- footer year -------------------------------------------------- */
const yr = document.getElementById('year');
if (yr) yr.textContent = String(new Date().getFullYear());

/* ---- mobile nav ------------------------------------------------- */
const navToggle = document.querySelector('.nav-toggle');
const nav = document.getElementById('nav');
navToggle?.addEventListener('click', () => {
  const open = nav.classList.toggle('is-open');
  navToggle.setAttribute('aria-expanded', String(open));
});
nav?.querySelectorAll('a').forEach(a => a.addEventListener('click', () => {
  nav.classList.remove('is-open');
  navToggle?.setAttribute('aria-expanded', 'false');
}));

/* ---- active section in nav ------------------------------------- */
const navLinks = new Map(
  [...(nav?.querySelectorAll('a[href^="#"]') ?? [])].map(a => [a.getAttribute('href').slice(1), a])
);
if (navLinks.size && 'IntersectionObserver' in window) {
  const spy = new IntersectionObserver((entries) => {
    entries.forEach(e => {
      const link = navLinks.get(e.target.id);
      if (link && e.isIntersecting) {
        navLinks.forEach(l => l.removeAttribute('aria-current'));
        link.setAttribute('aria-current', 'true');
      }
    });
  }, { rootMargin: '-45% 0px -50% 0px' });
  navLinks.forEach((_, id) => {
    const sec = document.getElementById(id);
    if (sec) spy.observe(sec);
  });
}

/* ---- reveal on scroll --------------------------------------- */
if ('IntersectionObserver' in window) {
  const io = new IntersectionObserver((entries, obs) => {
    entries.forEach(e => {
      if (e.isIntersecting) { e.target.classList.add('is-visible'); obs.unobserve(e.target); }
    });
  }, { rootMargin: '0px 0px -10% 0px' });
  document.querySelectorAll('.reveal').forEach(el => io.observe(el));
} else {
  document.querySelectorAll('.reveal').forEach(el => el.classList.add('is-visible'));
}

/* ---- contact form ----------------------------------------- */
const form = document.getElementById('contact-form');
form?.addEventListener('submit', async (e) => {
  e.preventDefault();
  const status = document.getElementById('cf-status');
  const submit = document.getElementById('cf-submit');
  const fd = new FormData(form);

  status.className = 'form-status';
  status.textContent = 'Sending…';
  submit.disabled = true;

  try {
    const res = await fetch(`${API}/contact`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: fd.get('name'),
        email: fd.get('email'),
        subject: fd.get('subject'),
        message: fd.get('message'),
        website: fd.get('website') // honeypot
      })
    });
    const data = await res.json().catch(() => ({}));
    if (res.ok && data.success) {
      status.className = 'form-status ok';
      status.textContent = data.message || 'Thanks — your message has been sent.';
      form.reset();
    } else {
      status.className = 'form-status err';
      status.textContent = data.error || `Something went wrong (${res.status}).`;
    }
  } catch (err) {
    console.error('contact:', err);
    status.className = 'form-status err';
    status.textContent = 'Network error — please email me instead.';
  } finally {
    submit.disabled = false;
  }
});

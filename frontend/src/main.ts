import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

declare global {
  interface Window {
    __APP_CONFIG__?: { apiBase?: string; wsBase?: string };
  }
}

async function loadRuntimeConfig() {
  try {
    const res = await fetch('/assets/runtime-config.json', { cache: 'no-store' });
    if (!res.ok) throw new Error('No runtime config');
    const cfg = await res.json();
    window.__APP_CONFIG__ = cfg;
  } catch (e) {
    // fallback to defaults
    window.__APP_CONFIG__ = { apiBase: '/api/v1', wsBase: '/hubs' };
  }
}

(async () => {
  await loadRuntimeConfig();
  bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err));
})();

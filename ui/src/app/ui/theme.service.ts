import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  theme = signal<Theme>('light');

  constructor() {
    const saved = localStorage.getItem('theme') as Theme | null;
    this.apply(saved ?? 'light');
  }

  toggle() {
    this.apply(this.theme() === 'light' ? 'dark' : 'light');
  }

  private apply(t: Theme) {
    this.theme.set(t);
    localStorage.setItem('theme', t);
    document.documentElement.classList.toggle('dark', t === 'dark');
  }
}

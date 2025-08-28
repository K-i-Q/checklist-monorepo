import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      class="inline-flex items-center gap-2 rounded-xl border border-slate-200 px-3 py-2 text-sm
              hover:bg-slate-50 active:scale-[.98]
              dark:border-slate-700 dark:hover:bg-slate-800"
      (click)="theme.toggle()"
      [attr.aria-label]="'Alternar tema: ' + theme.theme()"
    >
      <svg
        *ngIf="theme.theme() === 'light'"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        class="h-4 w-4 text-amber-500"
        fill="currentColor"
      >
        <path d="M12 18a6 6 0 1 0 0-12 6 6 0 0 0 0 12Z" />
        <path
          d="M12 2v2m0 16v2M4 12H2m20 0h-2M5.64 5.64 4.22 4.22m15.56 15.56-1.42-1.42M18.36 5.64l1.42-1.42M4.22 19.78l1.42-1.42"
        />
      </svg>
      <svg
        *ngIf="theme.theme() === 'dark'"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        class="h-4 w-4 text-sky-300"
        fill="currentColor"
      >
        <path d="M21 12.79A9 9 0 1 1 11.21 3a7 7 0 1 0 9.79 9.79Z" />
      </svg>
      <span class="hidden sm:inline">{{ theme.theme() === 'light' ? 'Light' : 'Dark' }}</span>
    </button>
  `,
})
export class ThemeToggleComponent {
  theme = inject(ThemeService);
}

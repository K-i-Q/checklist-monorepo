import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toasts',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed right-4 top-4 z-50 space-y-2">
      <div
        *ngFor="let t of toastService.toasts()"
        class="pointer-events-auto rounded-xl border px-4 py-3 shadow-lg backdrop-blur-sm"
        [ngClass]="{
          'bg-emerald-50 border-emerald-200 text-emerald-800': t.kind === 'success',
          'bg-rose-50 border-rose-200 text-rose-800': t.kind === 'error',
          'bg-sky-50 border-sky-200 text-sky-800': t.kind === 'info'
        }"
      >
        <div class="flex items-start gap-3">
          <div class="mt-0.5">
            <span *ngIf="t.kind === 'success'">✅</span>
            <span *ngIf="t.kind === 'error'">⚠️</span>
            <span *ngIf="t.kind === 'info'">ℹ️</span>
          </div>
          <div class="text-sm font-medium">{{ t.text }}</div>
          <button
            class="ml-3 text-slate-500 hover:text-slate-700"
            (click)="toastService.dismiss(t.id)"
          >
            ✕
          </button>
        </div>
      </div>
    </div>
  `,
})
export class ToastsComponent {
  toastService = inject(ToastService);
}

import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';
export interface Toast {
  id: number;
  kind: ToastKind;
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toasts = signal<Toast[]>([]);
  private seq = 0;
  toasts = this._toasts.asReadonly();

  private push(kind: ToastKind, text: string, ms = 3000) {
    const id = ++this.seq;
    this._toasts.update((arr) => [...arr, { id, kind, text }]);
    setTimeout(() => this.dismiss(id), ms);
  }

  success(text: string, ms = 2500) {
    this.push('success', text, ms);
  }
  error(text: string, ms = 3500) {
    this.push('error', text, ms);
  }
  info(text: string, ms = 3000) {
    this.push('info', text, ms);
  }

  dismiss(id: number) {
    this._toasts.update((arr) => arr.filter((t) => t.id !== id));
  }
  clear() {
    this._toasts.set([]);
  }
}

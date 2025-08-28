import { Injectable, signal } from '@angular/core';

export type AppRole = 'Executor' | 'Supervisor';
export interface AppUser {
  id: string;
  name: string;
  role: AppRole;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _user = signal<AppUser | null>(null);

  user = this._user.asReadonly();

  setUser(u: AppUser | null) {
    this._user.set(u);
  }

  isSupervisor(): boolean {
    return this._user()?.role === 'Supervisor';
  }
  idOrNull(): string | null {
    return this._user()?.id ?? null;
  }
}

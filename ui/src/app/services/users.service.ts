import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { AppUser, AppRole } from './auth.service';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private base = 'http://localhost:5095/api/checklists';

  constructor(private http: HttpClient) {}

  getUsers(): Observable<AppUser[]> {
    return this.http.get<{ id: string; name: string; role: string }[]>(`${this.base}/users`).pipe(
      map((list) =>
        list.map((u) => ({
          id: u.id,
          name: u.name,
          role: (u.role === 'Supervisor' ? 'Supervisor' : 'Executor') as AppRole,
        }))
      )
    );
  }
}

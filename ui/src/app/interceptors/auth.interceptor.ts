import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '../services/auth.service';

/** Injeta X-User-Id e X-User-Role a partir do usuário selecionado em memória */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const u = auth.user();
  if (!u) return next(req);

  const cloned = req.clone({
    setHeaders: {
      'X-User-Id': u.id,
      'X-User-Role': u.role,
    },
  });
  return next(cloned);
};

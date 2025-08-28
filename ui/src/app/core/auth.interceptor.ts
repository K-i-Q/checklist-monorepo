import { HttpInterceptorFn } from '@angular/common/http';

function read(key: string, fallback: string) {
  try {
    return localStorage.getItem(key) ?? fallback;
  } catch {
    return fallback;
  }
}

/** Injeta X-User-Id e X-User-Role em todas as requisições */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const userId = read('userId', '11111111-1111-1111-1111-111111111111'); // Executor (default)
  const userRole = read('userRole', 'Executor'); // ou 'Supervisor'
  const cloned = req.clone({
    setHeaders: {
      'X-User-Id': userId,
      'X-User-Role': userRole,
    },
  });
  return next(cloned);
};

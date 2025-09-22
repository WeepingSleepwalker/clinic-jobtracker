import { HttpInterceptorFn } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((err) => {
      const msg =
        err?.error?.message ||
        err?.error?.title || // ProblemDetails
        'Something went wrong';
      alert(msg); // keep it simple for the take-home
      return throwError(() => err);
    })
  );
};

import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
export type Doctor = { id: string; name: string; specialty?: string };

@Injectable({ providedIn: 'root' })
export class DoctorsApi {
  private http = inject(HttpClient);
  private base = `${environment.apiBase}/api/v1/doctors`;
  list() { return this.http.get<Doctor[]>(this.base); }
}

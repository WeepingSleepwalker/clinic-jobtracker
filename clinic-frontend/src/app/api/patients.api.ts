import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
export type Patient = { id: string; name: string };

@Injectable({ providedIn: 'root' })
export class PatientsApi {
  private http = inject(HttpClient);
  private base = `${environment.apiBase}/api/v1/patients`;
  create(body: { firstName: string; lastName: string; dob: string; email?: string }) {
    return this.http.post<any>(this.base, body);
  }
  list() { return this.http.get<Patient[]>(this.base); }
}

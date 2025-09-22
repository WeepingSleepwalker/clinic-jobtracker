import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AppointmentsApi {
  private http = inject(HttpClient);
  private base = `${environment.apiBase}/api/v1/appointments`;
  create(body: { patientId: string; doctorId: string; scheduledAt: string; durationMin: number }) {
    return this.http.post<any>(this.base, body);
  }
  completeAndInvoice(id: string, body: { amountCents: number; simulateBillingSuccess: boolean }) {
    return this.http.post<any>(`${this.base}/${id}/complete-and-invoice`, body);
  }
}

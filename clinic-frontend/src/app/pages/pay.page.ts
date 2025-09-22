import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, NonNullableFormBuilder } from '@angular/forms';
import { AppointmentsApi } from '../api/appointments.api';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  selector: 'app-pay',
  imports: [ReactiveFormsModule, CommonModule],
  template: `
    <h2>Pay & Receive Invoice</h2>
    <form [formGroup]="form" (ngSubmit)="submit()">
      <label>Appointment ID</label>
      <input formControlName="appointmentId" placeholder="paste appointment id">

      <label>Amount (cents)</label>
      <input type="number" formControlName="amountCents">

      <div style="margin-top:10px;">
        <button [disabled]="form.invalid || loading()">Pay Now</button>
      </div>
    </form>

    <div *ngIf="msg()" class="card">{{ msg() }}</div>
  `
})
export class PayPage {
  private fb = inject(NonNullableFormBuilder);
  private apptsApi = inject(AppointmentsApi);
  loading = signal(false);
  msg = signal('');

  form = this.fb.group({
    appointmentId: this.fb.control<string>(''),
    amountCents: this.fb.control<number>(15000),
  });

  submit() {
    this.msg.set(''); this.loading.set(true);
    const v = this.form.getRawValue();
    this.apptsApi.completeAndInvoice(v.appointmentId!, {
      amountCents: v.amountCents!, simulateBillingSuccess: true
    }).subscribe({
      next: (res:any) => { this.msg.set(`Invoice ${res.invoiceId} — ${res.invoiceStatus}`); this.loading.set(false); },
      error: (e:any) => { this.msg.set(e?.error?.message ?? 'Payment failed'); this.loading.set(false); }
    });
  }
}

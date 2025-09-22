
import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, NonNullableFormBuilder } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  standalone: true,
  selector: 'app-patient-new',
  imports: [ReactiveFormsModule, CommonModule],
  template: `
    <h2>Create Patient</h2>
    <form [formGroup]="form" (ngSubmit)="submit()">
      <label>First name</label>
      <input formControlName="firstName" placeholder="Jane">
      <label>Last name</label>
      <input formControlName="lastName" placeholder="Doe">
      <label>DOB</label>
      <input type="date" formControlName="dob">
      <label>Email (optional)</label>
      <input type="email" formControlName="email">
      <div style="margin-top:10px;"><button [disabled]="form.invalid || loading()">Create</button></div>
    </form>
    <div *ngIf="msg()" class="card">{{ msg() }}</div>
  `
})
export class PatientNewPage {
  private fb = inject(NonNullableFormBuilder);
  private http = inject(HttpClient);
  loading = signal(false);
  msg = signal('');

  form = this.fb.group({
    firstName: this.fb.control(''),
    lastName: this.fb.control(''),
    dob: this.fb.control(''),
    email: this.fb.control('')
  });

  submit() {
    this.msg.set(''); this.loading.set(true);
    this.http.post<any>(`${environment.apiBase}/api/v1/patients`, this.form.getRawValue())
      .subscribe({
        next: p => { this.msg.set(`Patient created: ${p.firstName} ${p.lastName} (${p.id})`); this.loading.set(false); },
        error: e => { this.msg.set(e?.error?.message ?? 'Failed'); this.loading.set(false); }
      });
  }
}

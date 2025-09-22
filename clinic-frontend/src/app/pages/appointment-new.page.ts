import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, NonNullableFormBuilder } from '@angular/forms';
import { DoctorsApi, Doctor } from '../api/doctors.api';
import { PatientsApi, Patient } from '../api/patients.api';
import { AppointmentsApi } from '../api/appointments.api';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  selector: 'app-appointment-new',
  imports: [ReactiveFormsModule, CommonModule],
  template: `
    <h2>Book Appointment</h2>
    <form [formGroup]="form" (ngSubmit)="submit()">
      <label>Patient</label>
      <select formControlName="patientId">
        <option value="">-- Select --</option>
        <option *ngFor="let p of patients()" [value]="p.id">{{ p.name }}</option>
      </select>

      <label>Doctor</label>
      <select formControlName="doctorId">
        <option value="">-- Select --</option>
        <option *ngFor="let d of doctors()" [value]="d.id">{{ d.name }} — {{ d.specialty || 'General' }}</option>
      </select>

      <label>Start (UTC)</label>
      <input type="datetime-local" formControlName="scheduledAt">

      <label>Duration (min)</label>
      <input type="number" formControlName="durationMin" min="5" max="120">

      <div style="margin-top:10px;"><button [disabled]="form.invalid || loading()">Create</button></div>
    </form>
    <div *ngIf="msg()" class="card">{{ msg() }}</div>
  `
})
export class AppointmentNewPage {
  private fb = inject(NonNullableFormBuilder);
  private doctorsApi = inject(DoctorsApi);
  private patientsApi = inject(PatientsApi);
  private apptsApi = inject(AppointmentsApi);

  doctors = signal<Doctor[]>([]);
  patients = signal<Patient[]>([]);
  loading = signal(false);
  msg = signal('');

  form = this.fb.group({
    patientId: this.fb.control<string>(''),
    doctorId: this.fb.control<string>(''),
    scheduledAt: this.fb.control<string>(''),
    durationMin: this.fb.control<number>(30),
  });

  constructor() {
    this.doctorsApi.list().subscribe(d => this.doctors.set(d));
    this.patientsApi.list().subscribe(p => this.patients.set(p));
    const dt = new Date(Date.now() + 10 * 60 * 1000);
    const isoLocal = new Date(dt.getTime() - dt.getTimezoneOffset()*60000).toISOString().slice(0,16);
    this.form.patchValue({ scheduledAt: isoLocal });
  }

  submit() {
    this.msg.set(''); this.loading.set(true);
    const v = this.form.getRawValue();
    const scheduledAt = new Date(v.scheduledAt as string).toISOString(); // to UTC Z
    this.apptsApi.create({ ...v, scheduledAt } as any).subscribe({
      next: (res) => { this.msg.set(`Appointment created: ${res.id}`); this.loading.set(false); },
      error: (e) => { this.msg.set(e?.error?.message ?? 'Failed'); this.loading.set(false); }
    });
  }
}

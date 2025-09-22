import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  standalone: true,
  selector: 'app-schedule',
  imports: [CommonModule, FormsModule],
  template: `
    <h2>Doctor Schedule</h2>
    <div class="row">
      <div>
        <label>Doctor</label>
        <select [(ngModel)]="doctorId">
          <option value="">-- Select --</option>
          <option *ngFor="let d of doctors()" [value]="d.id">
            {{ d.name }} — {{ d.specialty || 'General' }}
          </option>
        </select>
      </div>
      <div>
        <label>Date (UTC)</label>
        <input type="date" [(ngModel)]="date" />
      </div>
      <button (click)="load()">Load</button>
    </div>

    <div *ngIf="items()?.length === 0" class="card">No appointments for this date.</div>
    <div *ngFor="let a of items()" class="card">
      <div><strong>{{ a.patient.name }}</strong></div>
      <div>{{ a.scheduledAt }} → {{ a.end }}</div>
      <div>Status: {{ a.status }}</div>
      <div>ApptId: {{ a.id }}</div>
    </div>
  `
})
export class SchedulePage {
  private http = inject(HttpClient);

  doctors = signal<any[]>([]);
  doctorId = '';   // plain property, two-way bound
  date = new Date().toISOString().slice(0, 10); // YYYY-MM-DD
  items = signal<any[] | null>(null);

  constructor() {
    this.http.get<any[]>(`${environment.apiBase}/api/v1/doctors`)
      .subscribe(d => this.doctors.set(d));
  }

  load() {
    if (!this.doctorId) return;
    const params = new HttpParams().set('date', this.date);
    this.http.get<any>(`${environment.apiBase}/api/v1/doctors/${this.doctorId}/agenda`, { params })
      .subscribe(res => this.items.set(res.items ?? []));
  }
}

import { Routes } from '@angular/router';
import { PatientNewPage } from './pages/patient-new.page';
import { AppointmentNewPage } from './pages/appointment-new.page';
import { SchedulePage } from './pages/schedule.page';
import { PayPage } from './pages/pay.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'patients/new' },
  { path: 'patients/new', component: PatientNewPage, title: 'Create Patient' },
  { path: 'appointments/new', component: AppointmentNewPage, title: 'Book Appointment' },
  { path: 'pay', component: PayPage, title: 'Pay & Invoice' },
  { path: 'schedule', component: SchedulePage },       
  { path: '**', redirectTo: 'patients/new' }
];

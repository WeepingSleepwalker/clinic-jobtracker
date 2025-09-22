import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';



@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, CommonModule, FormsModule],
  template: `
    <header class="topbar">
      <h1>Clinic Demo</h1>
      <nav>
        <a routerLink="/patients/new">Create Patient</a>
        <a routerLink="/appointments/new">Book Appointment</a>
        <a routerLink="/pay">Pay & Invoice</a>
        <a routerLink="/schedule">Doctor's Schedule</a>
      </nav>
    </header>
    <main class="container"><router-outlet/></main>
  `,
  styles: [`
    .topbar{display:flex;gap:16px;align-items:center;padding:12px 20px;border-bottom:1px solid #eee}
    .topbar h1{margin:0;font-size:18px}
    nav a{margin-right:12px;text-decoration:none;color:#333}
    .container{max-width:860px;margin:18px auto;padding:0 12px}
  `]
})
export class AppComponent {}


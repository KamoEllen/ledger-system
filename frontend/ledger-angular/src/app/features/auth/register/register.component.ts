import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, RouterLink],
  template: `
    <div class="auth-page">
      <div class="particles">
        <div class="particle" *ngFor="let p of particles"></div>
      </div>
      <div class="auth-card glass-card">
        <div class="auth-logo">
          <span class="logo-icon">⬡</span>
          <h1>LedgerFlow</h1>
          <p>Secure Financial Ledger System</p>
        </div>

        <h2>Create account</h2>

        <div class="alert alert-error" *ngIf="error()">{{ error() }}</div>
        <div class="alert alert-success" *ngIf="success()">{{ success() }}</div>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="field">
            <label>Email</label>
            <input type="email" formControlName="email" placeholder="you@example.com" autocomplete="email">
            <span class="field-error" *ngIf="form.get('email')?.touched && form.get('email')?.invalid">
              Valid email required
            </span>
          </div>
          <div class="field">
            <label>Password</label>
            <input type="password" formControlName="password" placeholder="Min 8 chars" autocomplete="new-password">
            <span class="field-error" *ngIf="form.get('password')?.touched && form.get('password')?.invalid">
              Min 8 characters required
            </span>
          </div>
          <button type="submit" class="btn-primary" [disabled]="loading()">
            <span *ngIf="!loading()">Create Account</span>
            <span *ngIf="loading()" class="btn-spinner"></span>
          </button>
        </form>

        <p class="auth-link">
          Already registered? <a routerLink="/auth/login">Sign in</a>
        </p>
      </div>
    </div>
  `,
  styleUrls: ['../auth.styles.scss']
})
export class RegisterComponent {
  form: FormGroup;
  loading = signal(false);
  error = signal('');
  success = signal('');
  particles = Array(12).fill(0);

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    this.auth.register(this.form.value).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: err => {
        this.error.set(err.error?.message ?? 'Registration failed');
        this.loading.set(false);
      }
    });
  }
}

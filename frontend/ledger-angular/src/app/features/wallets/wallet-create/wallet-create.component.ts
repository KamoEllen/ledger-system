import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { WalletService } from '../../../core/services/wallet.service';

@Component({
  selector: 'app-wallet-create',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, RouterLink],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>Create Wallet</h1>
          <p class="subtitle">Add a new currency wallet to your account</p>
        </div>
        <a routerLink="/wallets" class="btn-outline">← Back</a>
      </div>

      <div class="form-card glass-card">
        <div class="alert alert-error" *ngIf="error()">{{ error() }}</div>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="field">
            <label>Currency Code</label>
            <input type="text" formControlName="currency" placeholder="USD, EUR, GBP…" maxlength="10">
            <span class="hint">3-letter ISO currency code (e.g. USD)</span>
            <span class="field-error" *ngIf="form.get('currency')?.touched && form.get('currency')?.invalid">
              Currency code required
            </span>
          </div>

          <div class="currency-presets">
            <button type="button" class="preset" *ngFor="let c of currencies" (click)="selectCurrency(c)">{{ c }}</button>
          </div>

          <div class="form-actions">
            <a routerLink="/wallets" class="btn-outline">Cancel</a>
            <button type="submit" class="btn-primary" [disabled]="loading()">
              <span *ngIf="!loading()">Create Wallet</span>
              <span *ngIf="loading()" class="btn-spinner"></span>
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styleUrls: ['../wallets.styles.scss']
})
export class WalletCreateComponent {
  form: FormGroup;
  loading = signal(false);
  error = signal('');
  currencies = ['USD', 'EUR', 'GBP', 'BTC', 'ETH', 'ZAR'];

  constructor(private fb: FormBuilder, private walletService: WalletService, private router: Router) {
    this.form = this.fb.group({ currency: ['', [Validators.required, Validators.minLength(1)]] });
  }

  selectCurrency(c: string): void {
    this.form.patchValue({ currency: c });
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    this.walletService.createWallet({ currency: this.form.value.currency.toUpperCase() }).subscribe({
      next: w => this.router.navigate(['/wallets', w.id]),
      error: err => { this.error.set(err.error?.message ?? 'Failed to create wallet'); this.loading.set(false); }
    });
  }
}

import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TransferService } from '../../../core/services/transfer.service';
import { WalletService } from '../../../core/services/wallet.service';
import { TransferResultDto } from '../../../core/models/transfer.models';
import { WalletDto } from '../../../core/models/wallet.models';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';

@Component({
  selector: 'app-transfer-create',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, RouterLink, LoadingComponent],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>New Transfer</h1>
          <p class="subtitle">Move funds between wallets</p>
        </div>
        <a routerLink="/transfers" class="btn-outline">← Back</a>
      </div>

      <app-loading *ngIf="walletsLoading()"></app-loading>

      <div class="transfer-layout" *ngIf="!walletsLoading()">
        <div class="form-card glass-card" *ngIf="!result()">
          <div class="alert alert-error" *ngIf="error()">{{ error() }}</div>

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="field">
              <label>Source Wallet</label>
              <select formControlName="sourceWalletId">
                <option value="">Select source wallet…</option>
                <option [value]="w.id" *ngFor="let w of activeWallets()">
                  {{ w.currency }} — {{ w.balance | number:'1.2-4' }} {{ w.isActive ? '' : '[FROZEN]' }}
                </option>
              </select>
              <span class="field-error" *ngIf="form.get('sourceWalletId')?.touched && form.get('sourceWalletId')?.invalid">
                Required
              </span>
            </div>

            <div class="field">
              <label>Destination Wallet ID</label>
              <input type="text" formControlName="destinationWalletId" placeholder="UUID of destination wallet">
              <span class="field-error" *ngIf="form.get('destinationWalletId')?.touched && form.get('destinationWalletId')?.invalid">
                Required
              </span>
            </div>

            <div class="two-col">
              <div class="field">
                <label>Amount</label>
                <input type="number" formControlName="amount" placeholder="0.00" step="0.01" min="0.01">
                <span class="field-error" *ngIf="form.get('amount')?.touched && form.get('amount')?.invalid">
                  Positive amount required
                </span>
              </div>
              <div class="field">
                <label>Currency</label>
                <input type="text" formControlName="currency" placeholder="USD" maxlength="10">
                <span class="field-error" *ngIf="form.get('currency')?.touched && form.get('currency')?.invalid">
                  Required
                </span>
              </div>
            </div>

            <div class="field">
              <label>Description <em>(optional)</em></label>
              <input type="text" formControlName="description" placeholder="Payment for…">
            </div>

            <div class="form-actions">
              <a routerLink="/transfers" class="btn-outline">Cancel</a>
              <button type="submit" class="btn-primary" [disabled]="loading()">
                <span *ngIf="!loading()">Send Transfer</span>
                <span *ngIf="loading()" class="btn-spinner"></span>
              </button>
            </div>
          </form>
        </div>

        <!-- Success result -->
        <div class="result-card glass-card success" *ngIf="result()">
          <div class="result-icon">✓</div>
          <h2>Transfer Successful!</h2>
          <div class="result-rows">
            <div class="result-row">
              <span>Transfer ID</span>
              <span class="mono">{{ result()!.transfer.id }}</span>
            </div>
            <div class="result-row">
              <span>Amount</span>
              <span class="highlight">{{ result()!.transfer.amount | number:'1.2-4' }} {{ result()!.transfer.currency }}</span>
            </div>
            <div class="result-row">
              <span>Status</span>
              <span class="status-badge status-completed">{{ result()!.transfer.status }}</span>
            </div>
            <div class="result-row">
              <span>Source Balance After</span>
              <span>{{ result()!.sourceBalanceAfter | number:'1.2-4' }}</span>
            </div>
            <div class="result-row">
              <span>Dest. Balance After</span>
              <span>{{ result()!.destinationBalanceAfter | number:'1.2-4' }}</span>
            </div>
          </div>
          <div class="result-actions">
            <button class="btn-outline" (click)="reset()">New Transfer</button>
            <a routerLink="/transfers" class="btn-primary">View All Transfers</a>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['../transfers.styles.scss']
})
export class TransferCreateComponent implements OnInit {
  form: FormGroup;
  wallets = signal<WalletDto[]>([]);
  activeWallets = signal<WalletDto[]>([]);
  walletsLoading = signal(true);
  loading = signal(false);
  error = signal('');
  result = signal<TransferResultDto | null>(null);

  constructor(
    private fb: FormBuilder,
    private transferService: TransferService,
    private walletService: WalletService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.form = this.fb.group({
      sourceWalletId: ['', Validators.required],
      destinationWalletId: ['', Validators.required],
      amount: [null, [Validators.required, Validators.min(0.0001)]],
      currency: ['', Validators.required],
      description: ['']
    });
  }

  ngOnInit(): void {
    this.walletService.getWallets().subscribe({
      next: w => {
        this.wallets.set(w);
        this.activeWallets.set(w.filter(x => x.isActive));
        this.walletsLoading.set(false);
        const from = this.route.snapshot.queryParamMap.get('from');
        if (from) this.form.patchValue({ sourceWalletId: from });
      },
      error: () => this.walletsLoading.set(false)
    });

    // Auto-fill currency when source wallet changes so it always matches
    this.form.get('sourceWalletId')!.valueChanges.subscribe(id => {
      const wallet = this.wallets().find(w => w.id === id);
      if (wallet) this.form.patchValue({ currency: wallet.currency });
    });
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    const v = this.form.value;
    this.transferService.createTransfer({
      sourceWalletId: v.sourceWalletId,
      destinationWalletId: v.destinationWalletId,
      amount: Number(v.amount),
      currency: v.currency.toUpperCase(),
      description: v.description || undefined
    }).subscribe({
      next: res => { this.result.set(res); this.loading.set(false); },
      // err.error is the parsed JSON body: { error: { code, message } }
      error: err => {
        this.error.set(err.error?.error?.message ?? 'Transfer failed');
        this.loading.set(false);
      }
    });
  }

  reset(): void {
    this.result.set(null);
    this.form.reset();
    this.error.set('');
  }
}
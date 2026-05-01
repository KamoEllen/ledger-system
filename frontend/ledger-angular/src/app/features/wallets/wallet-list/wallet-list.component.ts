import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { WalletService } from '../../../core/services/wallet.service';
import { WalletDto } from '../../../core/models/wallet.models';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';

@Component({
  selector: 'app-wallet-list',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingComponent],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>Wallets</h1>
          <p class="subtitle">Manage your financial accounts</p>
        </div>
        <a routerLink="/wallets/create" class="btn-primary">+ New Wallet</a>
      </div>

      <app-loading *ngIf="loading()"></app-loading>

      <div class="error-banner glass-card" *ngIf="error()">{{ error() }}</div>

      <div class="wallets-grid" *ngIf="!loading()">
        <a class="wallet-card glass-card" [routerLink]="['/wallets', w.id]" *ngFor="let w of wallets()">
          <div class="wallet-header">
            <div class="currency-tag">{{ w.currency }}</div>
            <span class="status-pill" [class.active]="w.isActive" [class.frozen]="!w.isActive">
              {{ w.isActive ? '● Active' : '● Frozen' }}
            </span>
          </div>
          <div class="balance">{{ w.balance | number:'1.2-4' }}</div>
          <div class="wallet-footer">
            <span class="id-text">{{ w.id | slice:0:8 }}…</span>
            <span class="date-text">{{ w.createdAt | date:'MMM d, y' }}</span>
          </div>
          <div class="card-glow" [class.frozen-glow]="!w.isActive"></div>
        </a>

        <a routerLink="/wallets/create" class="wallet-card wallet-add glass-card">
          <span class="add-icon">+</span>
          <span class="add-text">Add Wallet</span>
        </a>
      </div>

      <div class="empty-state glass-card" *ngIf="!loading() && !wallets().length">
        <p>No wallets yet. Create your first one!</p>
        <a routerLink="/wallets/create" class="btn-primary">Create Wallet</a>
      </div>
    </div>
  `,
  styleUrls: ['../wallets.styles.scss']
})
export class WalletListComponent implements OnInit {
  wallets = signal<WalletDto[]>([]);
  loading = signal(true);
  error = signal('');

  constructor(private walletService: WalletService) {}

  ngOnInit(): void {
    this.walletService.getWallets().subscribe({
      next: w => { this.wallets.set(w); this.loading.set(false); },
      error: err => { this.error.set(err.error?.message ?? 'Failed to load wallets'); this.loading.set(false); }
    });
  }
}

import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { WalletService } from '../../core/services/wallet.service';
import { TransferService } from '../../core/services/transfer.service';
import { WalletDto } from '../../core/models/wallet.models';
import { TransferDto } from '../../core/models/transfer.models';
import { LoadingComponent } from '../../shared/components/loading/loading.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingComponent],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>Dashboard</h1>
          <p class="subtitle">Welcome back, <span class="highlight">{{ userEmail() }}</span></p>
        </div>
        <div class="header-actions">
          <a routerLink="/wallets/create" class="btn-outline">+ New Wallet</a>
          <a routerLink="/transfers/create" class="btn-primary">⇄ Transfer</a>
        </div>
      </div>

      <app-loading *ngIf="loading()"></app-loading>

      <ng-container *ngIf="!loading()">
        <!-- Stats -->
        <div class="stats-grid">
          <div class="stat-card glass-card">
            <div class="stat-icon green">◈</div>
            <div class="stat-body">
              <span class="stat-value">{{ wallets().length }}</span>
              <span class="stat-label">Total Wallets</span>
            </div>
          </div>
          <div class="stat-card glass-card">
            <div class="stat-icon green">$</div>
            <div class="stat-body">
              <span class="stat-value">{{ totalBalance() | number:'1.2-2' }}</span>
              <span class="stat-label">Total Balance (USD equiv.)</span>
            </div>
          </div>
          <div class="stat-card glass-card">
            <div class="stat-icon blue">⇄</div>
            <div class="stat-body">
              <span class="stat-value">{{ recentTransfers().length }}</span>
              <span class="stat-label">Recent Transfers</span>
            </div>
          </div>
          <div class="stat-card glass-card">
            <div class="stat-icon red">⬡</div>
            <div class="stat-body">
              <span class="stat-value">{{ activeWallets() }}</span>
              <span class="stat-label">Active Wallets</span>
            </div>
          </div>
        </div>

        <!-- Wallets -->
        <div class="section">
          <div class="section-header">
            <h2>Your Wallets</h2>
            <a routerLink="/wallets" class="view-all">View all →</a>
          </div>
          <div class="wallets-grid" *ngIf="wallets().length; else noWallets">
            <a class="wallet-card glass-card" [routerLink]="['/wallets', w.id]" *ngFor="let w of wallets()">
              <div class="wallet-top">
                <span class="currency-badge">{{ w.currency }}</span>
                <span class="status-dot" [class.active]="w.isActive" [class.frozen]="!w.isActive"></span>
              </div>
              <div class="wallet-balance">{{ w.balance | number:'1.2-4' }}</div>
              <div class="wallet-meta">
                <span [class]="w.isActive ? 'badge-green' : 'badge-red'">{{ w.isActive ? 'Active' : 'Frozen' }}</span>
                <span class="wallet-date">{{ w.createdAt | date:'MMM d, y' }}</span>
              </div>
            </a>
          </div>
          <ng-template #noWallets>
            <div class="empty-state glass-card">
              <p>No wallets yet.</p>
              <a routerLink="/wallets/create" class="btn-primary">Create your first wallet</a>
            </div>
          </ng-template>
        </div>

        <!-- Recent transfers -->
        <div class="section">
          <div class="section-header">
            <h2>Recent Transfers</h2>
            <a routerLink="/transfers" class="view-all">View all →</a>
          </div>
          <div class="transfers-list glass-card" *ngIf="recentTransfers().length; else noTransfers">
            <div class="transfer-row" *ngFor="let t of recentTransfers()">
              <div class="transfer-icon" [class]="statusClass(t.status)">⇄</div>
              <div class="transfer-info">
                <span class="transfer-id">{{ t.id | slice:0:8 }}…</span>
                <span class="transfer-desc">{{ t.description || 'No description' }}</span>
              </div>
              <div class="transfer-amount">
                <span class="amount">{{ t.amount | number:'1.2-4' }} {{ t.currency }}</span>
                <span class="status" [class]="statusClass(t.status)">{{ t.status }}</span>
              </div>
            </div>
          </div>
          <ng-template #noTransfers>
            <div class="empty-state glass-card">
              <p>No transfers yet.</p>
              <a routerLink="/transfers/create" class="btn-primary">Make your first transfer</a>
            </div>
          </ng-template>
        </div>
      </ng-container>
    </div>
  `,
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  wallets = signal<WalletDto[]>([]);
  recentTransfers = signal<TransferDto[]>([]);
  loading = signal(true);

  userEmail = computed(() => this.auth.user()?.email ?? '');
  totalBalance = computed(() => this.wallets().reduce((s, w) => s + w.balance, 0));
  activeWallets = computed(() => this.wallets().filter(w => w.isActive).length);

  constructor(
    private auth: AuthService,
    private walletService: WalletService,
    private transferService: TransferService
  ) {}

  ngOnInit(): void {
    this.walletService.getWallets().subscribe({
      next: wallets => this.wallets.set(wallets),
      error: () => {}
    });
    this.transferService.getTransfers(1, 5).subscribe({
      next: res => {
        this.recentTransfers.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  statusClass(status: string): string {
    if (status === 'Completed') return 'status-completed';
    if (status === 'Failed') return 'status-failed';
    return 'status-pending';
  }
}

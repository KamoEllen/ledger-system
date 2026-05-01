import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { WalletService } from '../../../core/services/wallet.service';
import { WalletDetailDto, LedgerEntryDto, PagedResponse } from '../../../core/models/wallet.models';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-wallet-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingComponent, PaginationComponent],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>Wallet Detail</h1>
          <p class="subtitle">{{ wallet()?.currency }} Wallet</p>
        </div>
        <div class="header-actions">
          <a routerLink="/wallets" class="btn-outline">← Back</a>
          <a routerLink="/transfers/create" [queryParams]="{ from: wallet()?.id }" class="btn-primary">⇄ Transfer</a>
        </div>
      </div>

      <app-loading *ngIf="loading()"></app-loading>

      <ng-container *ngIf="!loading() && wallet()">
        <!-- Wallet info card -->
        <div class="wallet-hero glass-card">
          <div class="hero-left">
            <div class="currency-big">{{ wallet()!.currency }}</div>
            <div class="balance-big">{{ wallet()!.balance | number:'1.2-4' }}</div>
            <div class="wallet-id">ID: {{ wallet()!.id }}</div>
          </div>
          <div class="hero-right">
            <div class="info-row">
              <span class="info-label">Status</span>
              <span class="status-pill" [class.active]="wallet()!.isActive" [class.frozen]="!wallet()!.isActive">
                {{ wallet()!.isActive ? '● Active' : '● Frozen' }}
              </span>
            </div>
            <div class="info-row">
              <span class="info-label">Created</span>
              <span class="info-value">{{ wallet()!.createdAt | date:'medium' }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Owner ID</span>
              <span class="info-value mono">{{ wallet()!.userId | slice:0:8 }}…</span>
            </div>
          </div>
        </div>

        <!-- Ledger history -->
        <div class="section-header">
          <h2>Transaction History</h2>
          <span class="count-badge">{{ historyRes()?.totalCount ?? 0 }} entries</span>
        </div>

        <app-loading *ngIf="historyLoading()"></app-loading>

        <div class="history-table glass-card" *ngIf="!historyLoading() && history().length">
          <div class="table-header">
            <span>Type</span>
            <span>Amount</span>
            <span>Balance After</span>
            <span>Description</span>
            <span>Date</span>
          </div>
          <div class="table-row" *ngFor="let e of history()">
            <span class="entry-type" [class.credit]="e.entryType === 'Credit'" [class.debit]="e.entryType === 'Debit'">
              {{ e.entryType === 'Credit' ? '▲' : '▼' }} {{ e.entryType }}
            </span>
            <span class="entry-amount" [class.credit]="e.entryType === 'Credit'" [class.debit]="e.entryType === 'Debit'">
              {{ e.entryType === 'Credit' ? '+' : '-' }}{{ e.amount | number:'1.2-4' }}
            </span>
            <span class="balance-after">{{ e.balanceAfter | number:'1.2-4' }}</span>
            <span class="entry-desc">{{ e.description || '—' }}</span>
            <span class="entry-date">{{ e.createdAt | date:'MMM d, HH:mm' }}</span>
          </div>
        </div>

        <div class="empty-state glass-card" *ngIf="!historyLoading() && !history().length">
          <p>No transactions yet.</p>
        </div>

        <app-pagination
          [page]="page()"
          [totalPages]="historyRes()?.totalPages ?? 1"
          [hasPrevious]="historyRes()?.hasPreviousPage ?? false"
          [hasNext]="historyRes()?.hasNextPage ?? false"
          (pageChange)="loadHistory($event)">
        </app-pagination>
      </ng-container>
    </div>
  `,
  styleUrls: ['../wallets.styles.scss']
})
export class WalletDetailComponent implements OnInit {
  wallet = signal<WalletDetailDto | null>(null);
  history = signal<LedgerEntryDto[]>([]);
  historyRes = signal<PagedResponse<LedgerEntryDto> | null>(null);
  loading = signal(true);
  historyLoading = signal(false);
  page = signal(1);
  error = signal('');

  constructor(private route: ActivatedRoute, private walletService: WalletService) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.walletService.getWallet(id).subscribe({
      next: w => { this.wallet.set(w); this.loading.set(false); this.loadHistory(1); },
      error: err => { this.error.set(err.error?.message ?? 'Wallet not found'); this.loading.set(false); }
    });
  }

  loadHistory(page: number): void {
    this.page.set(page);
    this.historyLoading.set(true);
    const id = this.route.snapshot.paramMap.get('id')!;
    this.walletService.getHistory(id, page).subscribe({
      next: res => { this.historyRes.set(res); this.history.set(res.items); this.historyLoading.set(false); },
      error: () => this.historyLoading.set(false)
    });
  }
}

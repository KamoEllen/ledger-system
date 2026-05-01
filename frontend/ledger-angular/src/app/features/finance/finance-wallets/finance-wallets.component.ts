import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FinanceService } from '../../../core/services/finance.service';
import { WalletDto, PagedResponse } from '../../../core/models/wallet.models';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-finance-wallets',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingComponent, PaginationComponent],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>All Wallets</h1>
          <p class="subtitle">System-wide wallet overview</p>
        </div>
        <span class="role-badge">Finance</span>
      </div>

      <app-loading *ngIf="loading()"></app-loading>

      <div class="wallets-table glass-card" *ngIf="!loading() && wallets().length">
        <div class="table-header">
          <span>ID</span>
          <span>Owner</span>
          <span>Currency</span>
          <span>Balance</span>
          <span>Status</span>
          <span>Created</span>
        </div>
        <div class="table-row" *ngFor="let w of wallets()">
          <a [routerLink]="['/wallets', w.id]" class="mono link">{{ w.id | slice:0:8 }}…</a>
          <span class="mono text-muted">{{ w.userId | slice:0:8 }}…</span>
          <span class="currency-tag">{{ w.currency }}</span>
          <span class="balance-cell">{{ w.balance | number:'1.2-4' }}</span>
          <span class="status-pill" [class.active]="w.isActive" [class.frozen]="!w.isActive">
            {{ w.isActive ? 'Active' : 'Frozen' }}
          </span>
          <span class="date-text">{{ w.createdAt | date:'MMM d, y' }}</span>
        </div>
      </div>

      <div class="empty-state glass-card" *ngIf="!loading() && !wallets().length">
        <p>No wallets found.</p>
      </div>

      <app-pagination
        [page]="page()"
        [totalPages]="res()?.totalPages ?? 1"
        [hasPrevious]="res()?.hasPreviousPage ?? false"
        [hasNext]="res()?.hasNextPage ?? false"
        (pageChange)="load($event)">
      </app-pagination>
    </div>
  `,
  styleUrls: ['../finance.styles.scss']
})
export class FinanceWalletsComponent implements OnInit {
  wallets = signal<WalletDto[]>([]);
  res = signal<PagedResponse<WalletDto> | null>(null);
  loading = signal(true);
  page = signal(1);

  constructor(private financeService: FinanceService) {}

  ngOnInit(): void { this.load(1); }

  load(p: number): void {
    this.page.set(p);
    this.loading.set(true);
    this.financeService.getAllWallets(p).subscribe({
      next: r => { this.res.set(r); this.wallets.set(r.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}

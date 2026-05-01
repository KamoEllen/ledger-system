import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TransferService } from '../../../core/services/transfer.service';
import { TransferDto } from '../../../core/models/transfer.models';
import { PagedResponse } from '../../../core/models/wallet.models';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-transfer-list',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingComponent, PaginationComponent],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>Transfers</h1>
          <p class="subtitle">Your transaction history</p>
        </div>
        <a routerLink="/transfers/create" class="btn-primary">⇄ New Transfer</a>
      </div>

      <app-loading *ngIf="loading()"></app-loading>

      <div class="transfers-table glass-card" *ngIf="!loading() && transfers().length">
        <div class="table-header">
          <span>ID</span>
          <span>From</span>
          <span>To</span>
          <span>Amount</span>
          <span>Status</span>
          <span>Date</span>
          <span></span>
        </div>
        <div class="table-row" *ngFor="let t of transfers()">
          <span class="mono">{{ t.id | slice:0:8 }}…</span>
          <span class="mono text-muted">{{ t.sourceWalletId | slice:0:8 }}…</span>
          <span class="mono text-muted">{{ t.destinationWalletId | slice:0:8 }}…</span>
          <span class="amount">{{ t.amount | number:'1.2-4' }} <em>{{ t.currency }}</em></span>
          <span class="status-badge" [ngClass]="statusClass(t.status)">{{ t.status }}</span>
          <span class="date-text">{{ t.createdAt | date:'MMM d, HH:mm' }}</span>
          <a [routerLink]="['/transfers', t.id]" class="view-link">View →</a>
        </div>
      </div>

      <div class="empty-state glass-card" *ngIf="!loading() && !transfers().length">
        <p>No transfers yet.</p>
        <a routerLink="/transfers/create" class="btn-primary">Make your first transfer</a>
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
  styleUrls: ['../transfers.styles.scss']
})
export class TransferListComponent implements OnInit {
  transfers = signal<TransferDto[]>([]);
  res = signal<PagedResponse<TransferDto> | null>(null);
  loading = signal(true);
  page = signal(1);

  constructor(private transferService: TransferService) {}

  ngOnInit(): void { this.load(1); }

  load(p: number): void {
    this.page.set(p);
    this.loading.set(true);
    this.transferService.getTransfers(p).subscribe({
      next: r => { this.res.set(r); this.transfers.set(r.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  statusClass(s: string): string {
    if (s === 'Completed') return 'status-completed';
    if (s === 'Failed') return 'status-failed';
    return 'status-pending';
  }
}

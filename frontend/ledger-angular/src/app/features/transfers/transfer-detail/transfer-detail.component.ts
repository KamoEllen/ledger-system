import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TransferService } from '../../../core/services/transfer.service';
import { TransferDto } from '../../../core/models/transfer.models';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';

@Component({
  selector: 'app-transfer-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingComponent],
  template: `
    <div class="page">
      <div class="page-header">
        <h1>Transfer Detail</h1>
        <a routerLink="/transfers" class="btn-outline">← Back</a>
      </div>

      <app-loading *ngIf="loading()"></app-loading>

      <div class="detail-card glass-card" *ngIf="!loading() && transfer()">
        <div class="detail-hero">
          <div class="hero-amount">{{ transfer()!.amount | number:'1.2-4' }}</div>
          <div class="hero-currency">{{ transfer()!.currency }}</div>
          <span class="status-badge" [ngClass]="statusClass(transfer()!.status)">{{ transfer()!.status }}</span>
        </div>

        <div class="detail-grid">
          <div class="detail-item">
            <span class="detail-label">Transfer ID</span>
            <span class="detail-value mono">{{ transfer()!.id }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">Idempotency Key</span>
            <span class="detail-value mono">{{ transfer()!.idempotencyKey }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">Source Wallet</span>
            <a class="detail-value link" [routerLink]="['/wallets', transfer()!.sourceWalletId]">
              {{ transfer()!.sourceWalletId }}
            </a>
          </div>
          <div class="detail-item">
            <span class="detail-label">Destination Wallet</span>
            <a class="detail-value link" [routerLink]="['/wallets', transfer()!.destinationWalletId]">
              {{ transfer()!.destinationWalletId }}
            </a>
          </div>
          <div class="detail-item">
            <span class="detail-label">Description</span>
            <span class="detail-value">{{ transfer()!.description || '—' }}</span>
          </div>
          <div class="detail-item">
            <span class="detail-label">Created At</span>
            <span class="detail-value">{{ transfer()!.createdAt | date:'long' }}</span>
          </div>
          <div class="detail-item" *ngIf="transfer()!.completedAt">
            <span class="detail-label">Completed At</span>
            <span class="detail-value">{{ transfer()!.completedAt | date:'long' }}</span>
          </div>
        </div>
      </div>

      <div class="error-card glass-card" *ngIf="!loading() && !transfer()">
        Transfer not found.
      </div>
    </div>
  `,
  styleUrls: ['../transfers.styles.scss']
})
export class TransferDetailComponent implements OnInit {
  transfer = signal<TransferDto | null>(null);
  loading = signal(true);

  constructor(private route: ActivatedRoute, private transferService: TransferService) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.transferService.getTransfer(id).subscribe({
      next: t => { this.transfer.set(t); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  statusClass(s: string): string {
    if (s === 'Completed') return 'status-completed';
    if (s === 'Failed') return 'status-failed';
    return 'status-pending';
  }
}

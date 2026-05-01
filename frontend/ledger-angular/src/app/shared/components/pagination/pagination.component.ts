import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="pagination" *ngIf="totalPages > 1">
      <button class="page-btn" [disabled]="!hasPrevious" (click)="go(page - 1)">‹</button>
      <ng-container *ngFor="let p of pages">
        <span *ngIf="p === -1" class="ellipsis">…</span>
        <button *ngIf="p !== -1" class="page-btn" [class.active]="p === page" (click)="go(p)">{{ p }}</button>
      </ng-container>
      <button class="page-btn" [disabled]="!hasNext" (click)="go(page + 1)">›</button>
    </div>
  `,
  styles: [`
    .pagination {
      display: flex;
      gap: 6px;
      justify-content: center;
      margin-top: 24px;
      flex-wrap: wrap;
    }
    .page-btn {
      width: 36px;
      height: 36px;
      border-radius: 8px;
      background: rgba(255,255,255,0.05);
      border: 1px solid rgba(255,255,255,0.1);
      color: rgba(255,255,255,0.7);
      cursor: pointer;
      font-size: 14px;
      transition: all 0.2s;
      &:hover:not(:disabled) {
        background: rgba(0,230,118,0.1);
        border-color: rgba(0,230,118,0.3);
        color: #00e676;
      }
      &.active {
        background: rgba(0,230,118,0.2);
        border-color: #00e676;
        color: #00e676;
        font-weight: 700;
      }
      &:disabled { opacity: 0.3; cursor: not-allowed; }
    }
    .ellipsis { display:flex;align-items:center;color:rgba(255,255,255,0.3);font-size:14px; }
  `]
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() totalPages = 1;
  @Input() hasPrevious = false;
  @Input() hasNext = false;
  @Output() pageChange = new EventEmitter<number>();

  get pages(): number[] {
    const result: number[] = [];
    const total = this.totalPages;
    const cur = this.page;
    if (total <= 7) {
      for (let i = 1; i <= total; i++) result.push(i);
    } else {
      result.push(1);
      if (cur > 3) result.push(-1);
      for (let i = Math.max(2, cur - 1); i <= Math.min(total - 1, cur + 1); i++) result.push(i);
      if (cur < total - 2) result.push(-1);
      result.push(total);
    }
    return result;
  }

  go(p: number): void {
    if (p >= 1 && p <= this.totalPages) this.pageChange.emit(p);
  }
}

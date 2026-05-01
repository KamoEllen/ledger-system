import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { AdminUserDto } from '../../../core/models/admin.models';
import { WalletDto, PagedResponse } from '../../../core/models/wallet.models';
import { LoadingComponent } from '../../../shared/components/loading/loading.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingComponent, PaginationComponent, FormsModule],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>User Management</h1>
          <p class="subtitle">Manage all system users and roles</p>
        </div>
        <span class="role-badge admin">Admin</span>
      </div>

      <app-loading *ngIf="loading()"></app-loading>

      <div class="users-table glass-card" *ngIf="!loading()">
        <div class="table-header">
          <span>ID</span>
          <span>Email</span>
          <span>Role</span>
          <span>Created</span>
          <span>Actions</span>
        </div>
        <div class="table-row" *ngFor="let u of users()">
          <span class="mono">{{ u.id | slice:0:8 }}…</span>
          <span class="email-cell">{{ u.email }}</span>
          <span class="role-pill" [ngClass]="roleClass(u.role)">{{ u.role }}</span>
          <span class="date-text">{{ u.createdAt | date:'MMM d, y' }}</span>
          <div class="actions-cell">
            <select [(ngModel)]="roleEdits[u.id]" class="role-select">
              <option value="User">User</option>
              <option value="Finance">Finance</option>
              <option value="Admin">Admin</option>
            </select>
            <button class="btn-xs" (click)="updateRole(u)">Update</button>
          </div>
        </div>
      </div>

      <div class="empty-state glass-card" *ngIf="!loading() && !users().length">
        <p>No users found.</p>
      </div>

      <app-pagination
        [page]="page()"
        [totalPages]="res()?.totalPages ?? 1"
        [hasPrevious]="res()?.hasPreviousPage ?? false"
        [hasNext]="res()?.hasNextPage ?? false"
        (pageChange)="load($event)">
      </app-pagination>

      <!-- Wallet freeze section -->
      <div class="section-header" style="margin-top:32px">
        <h2>Wallet Control</h2>
        <p class="subtitle">Freeze or unfreeze any wallet by ID</p>
      </div>
      <div class="wallet-control glass-card">
        <div class="control-row">
          <input type="text" [(ngModel)]="walletIdInput" placeholder="Enter Wallet UUID…" class="control-input">
          <button class="btn-freeze" (click)="freeze()">Freeze</button>
          <button class="btn-unfreeze" (click)="unfreeze()">Unfreeze</button>
        </div>
        <div class="control-result" *ngIf="controlResult()">
          <span [class]="controlResult()!.isActive ? 'status-active' : 'status-frozen'">
            Wallet {{ controlResult()!.id | slice:0:8 }}… is now {{ controlResult()!.isActive ? 'Active' : 'Frozen' }}
          </span>
        </div>
        <div class="control-error" *ngIf="controlError()">{{ controlError() }}</div>
      </div>
    </div>
  `,
  styleUrls: ['../admin.styles.scss']
})
export class AdminUsersComponent implements OnInit {
  users = signal<AdminUserDto[]>([]);
  res = signal<PagedResponse<AdminUserDto> | null>(null);
  loading = signal(true);
  page = signal(1);
  roleEdits: Record<string, string> = {};
  walletIdInput = '';
  controlResult = signal<WalletDto | null>(null);
  controlError = signal('');

  constructor(private adminService: AdminService) {}

  ngOnInit(): void { this.load(1); }

  load(p: number): void {
    this.page.set(p);
    this.loading.set(true);
    this.adminService.getUsers(p).subscribe({
      next: r => {
        this.res.set(r);
        this.users.set(r.items);
        r.items.forEach(u => { this.roleEdits[u.id] = u.role; });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  updateRole(user: AdminUserDto): void {
    const role = this.roleEdits[user.id];
    if (!role) return;
    this.adminService.updateUserRole(user.id, { role }).subscribe({
      next: updated => {
        this.users.update(list => list.map(u => u.id === updated.id ? updated : u));
      }
    });
  }

  freeze(): void {
    if (!this.walletIdInput.trim()) return;
    this.controlError.set('');
    this.adminService.freezeWallet(this.walletIdInput.trim()).subscribe({
      next: w => this.controlResult.set(w),
      error: err => this.controlError.set(err.error?.message ?? 'Failed to freeze wallet')
    });
  }

  unfreeze(): void {
    if (!this.walletIdInput.trim()) return;
    this.controlError.set('');
    this.adminService.unfreezeWallet(this.walletIdInput.trim()).subscribe({
      next: w => this.controlResult.set(w),
      error: err => this.controlError.set(err.error?.message ?? 'Failed to unfreeze wallet')
    });
  }

  roleClass(role: string): string {
    if (role === 'Admin') return 'role-admin';
    if (role === 'Finance') return 'role-finance';
    return 'role-user';
  }
}
